namespace ClaudeVPNGate.Services;

public sealed record GuardSnapshot(bool IsAdmin, bool TunnelUp, bool Sealed, string Adapter, string Message, string Error, int Stopped, int Cut);

public sealed class GuardController
{
    private readonly VpnDetector _detector = new();
    private readonly PlatformGate _gate = new();
    private readonly object _lock = new();
    private Timer? _timer;
    private bool _wasUp;
    public AppSettings Settings { get; private set; } = AppSettings.Load();
    public GuardSnapshot Snapshot { get; private set; } = new(false, false, false, "", "", "", 0, 0);
    public IReadOnlyList<AdapterRow> Adapters => _detector.Adapters;
    public event Action? Changed;

    public void ApplyFast()
    {
        lock (_lock)
        {
            var admin = Privilege.IsAdmin();
            if (!admin)
            {
                Publish(false, false, "", "Administrator approval is required. Claude is not protected yet.");
                return;
            }
            _detector.Refresh(Settings.CustomAdapterKeywords, Settings.IgnoreAdapters);
            if (_detector.Tunnel is { } tunnel)
            {
                _gate.Lift();
                _wasUp = true;
                Publish(true, false, tunnel.Name, "Tunnel owns the route. Claude is open.");
            }
            else
            {
                _gate.Seal(full: false);
                _wasUp = false;
                Publish(false, true, "", "No tunnel on the default route. Claude is sealed.");
            }
        }
        if (Privilege.IsAdmin() && !_wasUp)
            Task.Run(() => { lock (_lock) _gate.Seal(full: true); Publish(_wasUp, _gate.Sealed, Snapshot.Adapter, Snapshot.Message); });
        if (Settings.LaunchAtLogin && Privilege.IsAdmin())
            try { Autostart.Set(true); } catch { /* logged in the UI via later errors */ }
    }

    public void Start()
    {
        _timer?.Dispose();
        _timer = new Timer(_ => Tick(), null, TimeSpan.FromSeconds(Settings.PollIntervalSec), TimeSpan.FromSeconds(Settings.PollIntervalSec));
    }

    public void Save(AppSettings settings)
    {
        Settings = settings;
        Settings.Save();
        if (Privilege.IsAdmin()) Autostart.Set(Settings.LaunchAtLogin);
        Start();
    }

    public void SealForExit()
    {
        _timer?.Dispose();
        if (!Privilege.IsAdmin()) return;
        lock (_lock) _gate.Seal(full: true);
    }

    private void Tick()
    {
        if (!Privilege.IsAdmin()) return;
        lock (_lock)
        {
            _detector.Refresh(Settings.CustomAdapterKeywords, Settings.IgnoreAdapters);
            var up = _detector.Tunnel is not null;
            if (up && !_wasUp) { _gate.Lift(); _wasUp = true; Publish(true, false, _detector.Tunnel!.Name, "Tunnel owns the route. Claude is open."); }
            else if (!up) { _gate.Seal(full: false); _wasUp = false; Publish(false, true, "", "No tunnel on the default route. Claude is sealed."); }
            else Publish(true, false, _detector.Tunnel!.Name, Snapshot.Message);
        }
    }

    private void Publish(bool up, bool isSealed, string adapter, string message)
    {
        Snapshot = new(Privilege.IsAdmin(), up, isSealed && Privilege.IsAdmin(), adapter, message, _gate.Error, _gate.Stopped, _gate.Cut);
        Changed?.Invoke();
    }
}
