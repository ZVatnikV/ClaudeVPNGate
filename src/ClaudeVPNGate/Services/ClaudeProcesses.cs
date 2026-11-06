using System.Diagnostics;

namespace ClaudeVPNGate.Services;

public static class ClaudeProcesses
{
    public static int Stop()
    {
        var stopped = 0;
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (!IsClaude(process))
                    continue;
                process.Kill(entireProcessTree: true);
                stopped++;
            }
            catch
            {
                // Access denied or already exited.
            }
            finally
            {
                process.Dispose();
            }
        }

        return stopped;
    }

    public static List<string> DiscoverExecutables()
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in Roots())
        {
            if (!Directory.Exists(root))
                continue;
            try
            {
                foreach (var path in Directory.EnumerateFiles(root, "*.exe", SearchOption.AllDirectories)
                             .Concat(Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                                 .Where(path => Path.GetFileName(path).Equals("claude", StringComparison.OrdinalIgnoreCase)
                                                || path.EndsWith(".app/Contents/MacOS/Claude", StringComparison.OrdinalIgnoreCase))))
                {
                    if (IsClaudePath(path))
                        found.Add(path);
                    if (found.Count >= 40)
                        return found.ToList();
                }
            }
            catch
            {
                // Unreadable directory.
            }
        }

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var path = process.MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(path) && IsClaudePath(path))
                    found.Add(path);
            }
            catch
            {
                // System process.
            }
            finally
            {
                process.Dispose();
            }
        }

        return found.ToList();
    }

    private static bool IsClaude(Process process)
    {
        if (process.Id == Environment.ProcessId)
            return false;
        string path;
        try
        {
            path = process.MainModule?.FileName ?? "";
        }
        catch
        {
            path = "";
        }

        if (IsClaudePath(path) || IsClaudePath(process.ProcessName))
            return true;

        var name = process.ProcessName.ToLowerInvariant();
        if (name is not ("node" or "node.exe"))
            return false;
        var command = CommandLine(process);
        return command.Contains("anthropic", StringComparison.OrdinalIgnoreCase)
               || command.Contains("claude-code", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsClaudePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        var low = path.ToLowerInvariant();
        if (low.Contains("claudevpngate", StringComparison.Ordinal))
            return false;
        var name = Path.GetFileName(low);
        if (name is "claude.exe" or "claude" or "claude desktop.exe")
            return true;
        return low.Contains("anthropic", StringComparison.Ordinal)
               || low.Contains("claude-code", StringComparison.Ordinal)
               || low.Contains("@anthropic-ai", StringComparison.Ordinal);
    }

    private static string CommandLine(Process process)
    {
        if (OperatingSystem.IsLinux())
        {
            try
            {
                var raw = File.ReadAllBytes($"/proc/{process.Id}/cmdline");
                return System.Text.Encoding.UTF8.GetString(raw).Replace('\0', ' ');
            }
            catch
            {
                return "";
            }
        }

        if (OperatingSystem.IsWindows())
        {
            var text = ProcessRunner.Run(
                "powershell",
                $"-NoProfile -NonInteractive -Command \"(Get-CimInstance Win32_Process -Filter \\\"ProcessId={process.Id}\\\").CommandLine\"");
            return text;
        }

        return ProcessRunner.Run("ps", $"-o args= -p {process.Id}");
    }

    private static IEnumerable<string> Roots()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (OperatingSystem.IsWindows())
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            yield return Path.Combine(local, "AnthropicClaude");
            yield return Path.Combine(local, "Programs", "Claude");
            yield return Path.Combine(roaming, "Claude");
            yield return Path.Combine(roaming, "npm", "node_modules", "@anthropic-ai");
            yield break;
        }

        if (OperatingSystem.IsMacOS())
        {
            yield return "/Applications/Claude.app";
            yield return Path.Combine(home, "Applications", "Claude.app");
            yield return Path.Combine(home, "Library", "Application Support", "Claude");
            yield return "/usr/local/bin";
            yield break;
        }

        yield return "/usr/bin";
        yield return "/usr/local/bin";
        yield return "/opt";
        yield return Path.Combine(home, ".local", "bin");
        yield return Path.Combine(home, ".npm");
    }
}
