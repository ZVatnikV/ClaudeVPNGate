namespace ClaudeVPNGate.Services;

public static class Autostart
{
    private const string Task = "ClaudeVPNGate";

    public static void Set(bool enabled)
    {
        var exe = Environment.ProcessPath ?? "";
        if (exe.Length == 0) return;
        if (OperatingSystem.IsWindows())
        {
            if (enabled)
                ProcessRunner.Run("schtasks", $"/Create /F /TN \"{Task}\" /SC ONLOGON /RL HIGHEST /TR \"\\\"{exe}\\\"\"");
            else
                ProcessRunner.Run("schtasks", $"/Delete /TN \"{Task}\" /F");
            return;
        }
        if (OperatingSystem.IsMacOS())
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents");
            var path = Path.Combine(dir, "com.zvatnikv.claudevpngate.plist");
            if (!enabled) { if (File.Exists(path)) File.Delete(path); return; }
            Directory.CreateDirectory(dir);
            File.WriteAllText(path, $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
                <plist version="1.0"><dict>
                <key>Label</key><string>com.zvatnikv.claudevpngate</string>
                <key>RunAtLoad</key><true/>
                <key>ProgramArguments</key><array><string>{exe}</string></array>
                </dict></plist>
                """);
            return;
        }
        var home = AppSettings.DirectoryPath();
        var auto = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "autostart");
        var desktop = Path.Combine(auto, "claude-vpn-gate.desktop");
        _ = home;
        if (!enabled) { if (File.Exists(desktop)) File.Delete(desktop); return; }
        Directory.CreateDirectory(auto);
        var cmd = Privilege.IsAdmin() ? exe : $"pkexec \"{exe}\"";
        File.WriteAllText(desktop, $"[Desktop Entry]\nType=Application\nName=Claude VPN Gate\nExec={cmd}\nX-GNOME-Autostart-enabled=true\n");
    }
}
