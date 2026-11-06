using System.Runtime.InteropServices;

namespace ClaudeVPNGate.Services;

public static class SingleInstance
{
    private static Mutex? _mutex;
    private static FileStream? _lock;

    public static bool TryAcquire()
    {
        if (OperatingSystem.IsWindows())
        {
            _mutex = new Mutex(true, @"Local\ClaudeVPNGateSingleton", out var created);
            if (created)
                return true;
            _mutex.Dispose();
            _mutex = null;
            return false;
        }

        var path = Path.Combine(Path.GetTempPath(), "claude-vpn-gate.lock");
        try
        {
            _lock = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public static void Release()
    {
        _mutex?.Dispose();
        _mutex = null;
        _lock?.Dispose();
        _lock = null;
    }
}
