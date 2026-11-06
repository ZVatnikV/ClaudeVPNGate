using Avalonia;
using ClaudeVPNGate.Services;

namespace ClaudeVPNGate;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (!SingleInstance.TryAcquire())
            return;

        var guard = new GuardController();
        App.Guard = guard;
        guard.ApplyFast();
        App.BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        SingleInstance.Release();
    }
}
