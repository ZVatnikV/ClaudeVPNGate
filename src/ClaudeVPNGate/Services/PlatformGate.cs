using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ClaudeVPNGate.Services;

public sealed class PlatformGate
{
    private const string Rule = "ClaudeVPNGate Outbound";
    private const string Comment = "ClaudeVPNGate";
    private readonly HashSet<string> _ips = [];
    private DateTime _ipsAt;
    private DateTime _fullAt;
    private readonly Dictionary<string, string?> _doh = [];

    public bool Sealed { get; private set; }
    public string Error { get; private set; } = "";
    public int Stopped { get; private set; }
    public int Cut { get; private set; }
    public int Programs { get; private set; }

    public void Seal(bool full)
    {
        Sealed = true;
        Safe("hosts", () => WriteHosts(true));
        Safe("process", () => Stopped += ClaudeProcesses.Stop());
        if (!full && _fullAt != DateTime.MinValue && (DateTime.UtcNow - _fullAt).TotalSeconds < 5)
            return;
        _fullAt = DateTime.UtcNow;
        Safe("sessions", () => Cut += CutSessions());
        if (OperatingSystem.IsWindows())
        {
            Safe("nrpt", SetNrpt);
            Safe("doh", () => SetDoH(true));
            Safe("firewall", BlockPrograms);
        }
        else
            Safe("doh", () => BrowserPolicy(true));
    }

    public void Lift()
    {
        var errors = new List<string>();
        void Step(string name, Action action)
        {
            try { action(); }
            catch (Exception ex) { errors.Add(name + ": " + ex.Message); }
        }
        if (OperatingSystem.IsWindows())
        {
            Step("firewall", () => ProcessRunner.Run("netsh", $"advfirewall firewall delete rule name=\"{Rule}\""));
            Step("nrpt", ClearNrpt);
            Step("doh", () => SetDoH(false));
        }
        else Step("doh", () => BrowserPolicy(false));
        Step("hosts", () => WriteHosts(false));
        Sealed = false;
        Programs = 0;
        _fullAt = DateTime.MinValue;
        Error = string.Join("; ", errors);
    }

    private void Safe(string name, Action action)
    {
        try { action(); }
        catch (Exception ex) { Error = name + ": " + ex.Message; }
    }

    private static string HostsPath() => OperatingSystem.IsWindows()
        ? Path.Combine(Environment.SystemDirectory, "drivers", "etc", "hosts")
        : "/etc/hosts";

    private static void WriteHosts(bool seal)
    {
        var path = HostsPath();
        var text = File.ReadAllText(path);
        var updated = HostsEditor.Upsert(text, seal ? HostsEditor.RenderBlock() : null);
        if (updated == text) return;
        File.WriteAllText(path, updated);
        if (OperatingSystem.IsWindows()) ProcessRunner.Run("ipconfig", "/flushdns");
        else if (OperatingSystem.IsMacOS())
        {
            ProcessRunner.Run("dscacheutil", "-flushcache");
            ProcessRunner.Run("killall", "-HUP mDNSResponder");
        }
        else ProcessRunner.Run("resolvectl", "flush-caches");
    }

    private int CutSessions()
    {
        if (_ips.Count == 0 || (DateTime.UtcNow - _ipsAt).TotalSeconds > 30)
        {
            var found = DnsLookup.ResolveClaudeIpv4();
            if (found.Count > 0) { _ips.Clear(); foreach (var ip in found) _ips.Add(ip); _ipsAt = DateTime.UtcNow; }
        }
        if (_ips.Count == 0) return 0;
        if (OperatingSystem.IsWindows()) return WinTcp.Reset(_ips);
        var n = 0;
        if (OperatingSystem.IsLinux())
            foreach (var ip in _ips)
                if (ProcessRunner.RunCode("ss", "-K dst " + ip) == 0) n++;
        return n;
    }

    private void BlockPrograms()
    {
        var programs = ClaudeProcesses.DiscoverExecutables();
        Programs = programs.Count;
        var shown = ProcessRunner.Run("netsh", $"advfirewall firewall show rule name=\"{Rule}\" verbose");
        foreach (var program in programs)
        {
            if (!File.Exists(program) || shown.Contains(program, StringComparison.OrdinalIgnoreCase)) continue;
            ProcessRunner.RunCode("netsh", $"advfirewall firewall add rule name=\"{Rule}\" dir=out action=block program=\"{program}\" enable=yes profile=any");
        }
    }

    private static void SetNrpt()
    {
        var cmd = $"$e=@(Get-DnsClientNrptRule -EA SilentlyContinue|? {{ $_.Comment -eq '{Comment}' }}); if($e.Count -gt 0){{return}};";
        foreach (var s in new[] { "claude.ai", "anthropic.com", "claude.com" })
            cmd += $" Add-DnsClientNrptRule -Namespace '{s}' -NameServers 127.0.0.1 -Comment '{Comment}'; Add-DnsClientNrptRule -Namespace '.{s}' -NameServers 127.0.0.1 -Comment '{Comment}';";
        ProcessRunner.Run("powershell", "-NoProfile -NonInteractive -Command \"" + cmd + "\"");
    }

    private static void ClearNrpt() => ProcessRunner.Run("powershell",
        $"-NoProfile -NonInteractive -Command \"Get-DnsClientNrptRule -EA SilentlyContinue | ? {{ $_.Comment -eq '{Comment}' }} | Remove-DnsClientNrptRule -Force\"");

    private void SetDoH(bool seal)
    {
        string[] keys = [@"SOFTWARE\Policies\Google\Chrome", @"SOFTWARE\Policies\Microsoft\Edge", @"SOFTWARE\Policies\BraveSoftware\Brave"];
        foreach (var path in keys)
        {
            if (seal) { _doh.TryAdd(path, ReadReg(path)); WriteReg(path, "off"); }
            else if (_doh.TryGetValue(path, out var prior) && !string.IsNullOrEmpty(prior)) WriteReg(path, prior);
            else DeleteReg(path);
        }
    }

    private static string? ReadReg(string path) { try { using var k = Registry.LocalMachine.OpenSubKey(path); return k?.GetValue("DnsOverHttpsMode") as string; } catch { return null; } }
    private static void WriteReg(string path, string value) { using var k = Registry.LocalMachine.CreateSubKey(path); k?.SetValue("DnsOverHttpsMode", value); }
    private static void DeleteReg(string path) { try { using var k = Registry.LocalMachine.OpenSubKey(path, true); k?.DeleteValue("DnsOverHttpsMode", false); } catch { } }

    private static void BrowserPolicy(bool seal)
    {
        foreach (var dir in new[] { "/etc/opt/chrome/policies/managed", "/etc/chromium/policies/managed", "/etc/opt/edge/policies/managed", "/etc/brave/policies/managed" })
        {
            var path = Path.Combine(dir, "claude-vpn-gate.json");
            if (!seal) { if (File.Exists(path)) File.Delete(path); continue; }
            Directory.CreateDirectory(dir);
            File.WriteAllText(path, "{\"DnsOverHttpsMode\":\"off\"}\n");
        }
    }
}

internal static class WinTcp
{
    public static int Reset(HashSet<string> ips)
    {
        if (!OperatingSystem.IsWindows()) return 0;
        var wanted = ips.Select(DnsLookup.Ipv4Dword).ToHashSet();
        var size = 0;
        GetExtendedTcpTable(IntPtr.Zero, ref size, false, 2, 5, 0);
        if (size <= 0) return 0;
        var buffer = Marshal.AllocHGlobal(size);
        var cut = 0;
        try
        {
            if (GetExtendedTcpTable(buffer, ref size, false, 2, 5, 0) != 0) return 0;
            var count = Marshal.ReadInt32(buffer);
            for (var i = 0; i < count; i++)
            {
                var at = 4 + i * 24;
                var remote = Marshal.ReadInt32(buffer, at + 12);
                if (!wanted.Contains(remote)) continue;
                var row = new Row
                {
                    State = 12,
                    Local = Marshal.ReadInt32(buffer, at + 4),
                    LocalPort = Marshal.ReadInt32(buffer, at + 8),
                    Remote = remote,
                    RemotePort = Marshal.ReadInt32(buffer, at + 16),
                };
                if (SetTcpEntry(ref row) == 0) cut++;
            }
        }
        finally { Marshal.FreeHGlobal(buffer); }
        return cut;
    }

    [StructLayout(LayoutKind.Sequential)] private struct Row { public int State, Local, LocalPort, Remote, RemotePort; }
    [DllImport("iphlpapi.dll")] private static extern uint GetExtendedTcpTable(IntPtr table, ref int size, bool order, int family, int cls, uint reserved);
    [DllImport("iphlpapi.dll")] private static extern uint SetTcpEntry(ref Row row);
}
