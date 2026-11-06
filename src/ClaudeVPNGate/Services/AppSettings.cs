using System.Text.Json;

namespace ClaudeVPNGate.Services;

public sealed class AppSettings
{
    public string Language { get; set; } = "en";
    public double PollIntervalSec { get; set; } = 0.8;
    public bool LaunchAtLogin { get; set; } = true;
    public List<string> CustomAdapterKeywords { get; set; } = [];
    public List<string> IgnoreAdapters { get; set; } = [];

    public static string DirectoryPath()
    {
        var home = HomeDirectory();
        if (OperatingSystem.IsWindows())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClaudeVPNGate");
        if (OperatingSystem.IsMacOS())
            return Path.Combine(home, "Library", "Application Support", "ClaudeVPNGate");
        var xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrWhiteSpace(xdg) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SUDO_USER")))
            return Path.Combine(xdg, "claude-vpn-gate");
        return Path.Combine(home, ".config", "claude-vpn-gate");
    }

    public static AppSettings Load()
    {
        var path = Path.Combine(DirectoryPath(), "settings.json");
        try
        {
            if (!File.Exists(path))
                return new AppSettings();
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Language = Language == "ru" ? "ru" : "en";
        PollIntervalSec = Math.Clamp(PollIntervalSec, 0.4, 5);
        var dir = DirectoryPath();
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(dir, "settings.json"), json);
    }

    private static string HomeDirectory()
    {
        var sudoUser = Environment.GetEnvironmentVariable("SUDO_USER");
        if (!string.IsNullOrWhiteSpace(sudoUser) && !sudoUser.Equals("root", StringComparison.OrdinalIgnoreCase))
        {
            var root = OperatingSystem.IsMacOS() ? "/Users" : "/home";
            var path = Path.Combine(root, sudoUser);
            if (Directory.Exists(path))
                return path;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }
}
