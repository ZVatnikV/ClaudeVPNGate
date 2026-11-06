namespace ClaudeVPNGate.Services;

public static class VpnClassifier
{
    public static readonly string[] Keywords =
    [
        "wireguard", "wintun", "tun", "tap-windows", "tap adapter", "openvpn",
        "nordlynx", "nordvpn", "mullvad", "warp", "protonvpn", "proton",
        "expressvpn", "surfshark", "cyberghost", "private internet access",
        "anyconnect", "cisco vpn", "forticlient", "globalprotect", "palo alto",
        "softether", "zerotier", "tailscale", "outline", "sing-box", "clash",
        "tun2socks", "sstp", "l2tp", "pptp", "ikev2", "amnezia", "happ", "vpn",
        "wg", "utun", "ipsec", "ppp",
    ];

    public static readonly string[] Ignore =
    [
        "loopback", "bluetooth", "vethernet", "hyper-v", "vmware", "virtualbox",
        "docker", "wsl", "xbox", "wi-fi direct", "npcap", "pseudo-interface",
        "teredo", "isatap", "wan miniport", "radmin", "famatech", "hamachi", "logmein",
    ];

    public static readonly string[] Physical =
    [
        "беспровод", "wi-fi", "wifi", "wireless", "ethernet", "локальн",
        "tp-link", "realtek", "intel", "broadcom", "killer",
    ];

    public static bool IsTunnel(string name, string? description, IEnumerable<string>? extraKeywords, IEnumerable<string>? extraIgnore)
    {
        var blob = $"{name} {description}".Trim().ToLowerInvariant();
        if (Hit(blob, Ignore) || Hit(blob, extraIgnore))
            return false;
        if (Hit(blob, Keywords) || Hit(blob, extraKeywords))
            return true;

        var token = (name ?? "").Trim().ToLowerInvariant();
        if (token.StartsWith("utun", StringComparison.Ordinal)
            || token.StartsWith("tun", StringComparison.Ordinal)
            || token.StartsWith("tap", StringComparison.Ordinal)
            || token.StartsWith("wg", StringComparison.Ordinal)
            || token.StartsWith("ppp", StringComparison.Ordinal)
            || token.StartsWith("ipsec", StringComparison.Ordinal))
            return true;

        if (Hit(blob, Physical))
            return false;
        if (System.Text.RegularExpressions.Regex.IsMatch(token, @"^(en|eth|wlan|wlp|wlx)\d"))
            return false;

        // Unknown default route stays sealed. Add a keyword in Settings if this is your tunnel.
        return false;
    }

    private static bool Hit(string blob, IEnumerable<string>? needles)
    {
        if (needles is null)
            return false;
        foreach (var needle in needles)
        {
            var item = (needle ?? "").Trim().ToLowerInvariant();
            if (item.Length > 0 && blob.Contains(item, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
