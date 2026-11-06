using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace ClaudeVPNGate.Services;

public sealed record AdapterRow(string Name, string Description, bool IsUp, IReadOnlyList<string> Addresses);

public sealed class VpnDetector
{
    public IReadOnlyList<AdapterRow> Adapters { get; private set; } = [];
    public AdapterRow? Tunnel { get; private set; }

    public void Refresh(IEnumerable<string>? keywords, IEnumerable<string>? ignore)
    {
        var adapters = ListAdapters();
        Adapters = adapters;
        Tunnel = null;

        var winner = DefaultRouteName();
        if (!string.IsNullOrWhiteSpace(winner))
        {
            var row = adapters.FirstOrDefault(item =>
                item.Name.Equals(winner, StringComparison.OrdinalIgnoreCase)
                || item.Description.Equals(winner, StringComparison.OrdinalIgnoreCase));
            var description = row?.Description ?? winner;
            var name = row?.Name ?? winner;
            if (row is { IsUp: false })
                return;
            if (VpnClassifier.IsTunnel(name, description, keywords, ignore))
                Tunnel = row ?? new AdapterRow(name, description, true, []);
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            var ras = RasName();
            if (!string.IsNullOrWhiteSpace(ras))
                Tunnel = new AdapterRow("RAS:" + ras, ras, true, []);
        }
    }

    public static IReadOnlyList<AdapterRow> ListAdapters()
    {
        var rows = new List<AdapterRow>();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            var addresses = nic.GetIPProperties().UnicastAddresses
                .Select(item => item.Address.ToString())
                .Where(item => !item.StartsWith("fe80:", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            rows.Add(new AdapterRow(
                nic.Name,
                nic.Description,
                nic.OperationalStatus == OperationalStatus.Up,
                addresses));
        }

        return rows;
    }

    private static string? DefaultRouteName()
    {
        if (OperatingSystem.IsWindows())
            return WindowsDefault();
        if (OperatingSystem.IsMacOS())
            return MacDefault();
        return LinuxDefault();
    }

    private static string? WindowsDefault()
    {
        var text = ProcessRunner.Run("route", "print -4");
        string? bestName = null;
        var bestMetric = int.MaxValue;
        foreach (var line in text.Split('\n'))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5 || parts[0] != "0.0.0.0" || parts[1] != "0.0.0.0")
                continue;
            if (!int.TryParse(parts[4], out var metric))
                continue;
            if (metric >= bestMetric)
                continue;
            var ip = parts[3];
            var match = ListAdapters().FirstOrDefault(item => item.Addresses.Contains(ip));
            if (match is null)
                continue;
            bestMetric = metric;
            bestName = match.Name;
        }

        return bestName;
    }

    private static string? LinuxDefault()
    {
        var text = ProcessRunner.Run("ip", "-4 route show default");
        string? best = null;
        var bestMetric = int.MaxValue;
        foreach (var line in text.Split('\n'))
        {
            if (!line.TrimStart().StartsWith("default ", StringComparison.Ordinal))
                continue;
            var dev = Regex.Match(line, @"\bdev\s+(\S+)");
            if (!dev.Success)
                continue;
            var metricMatch = Regex.Match(line, @"\bmetric\s+(\d+)");
            var metric = metricMatch.Success ? int.Parse(metricMatch.Groups[1].Value) : 0;
            if (metric >= bestMetric)
                continue;
            bestMetric = metric;
            best = dev.Groups[1].Value;
        }

        return best;
    }

    private static string? MacDefault()
    {
        var text = ProcessRunner.Run("route", "-n get default");
        var match = Regex.Match(text, @"interface:\s*(\S+)");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? RasName()
    {
        var text = ProcessRunner.Run("rasdial", "");
        if (Regex.IsMatch(text, "нет подключен|no connections", RegexOptions.IgnoreCase))
            return null;
        var match = Regex.Match(text, @"(?:connected to|подключен[оа]?\s+к)\s+(.+)", RegexOptions.IgnoreCase);
        if (!match.Success)
            return null;
        var name = match.Groups[1].Value.Trim().TrimEnd('.').Trim('"');
        return name.Length == 0 ? null : name;
    }
}
