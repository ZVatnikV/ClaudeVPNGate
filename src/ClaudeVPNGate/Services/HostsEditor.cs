namespace ClaudeVPNGate.Services;

public static class HostsEditor
{
    public const string Begin = "# BEGIN ClaudeVPNGate";
    public const string End = "# END ClaudeVPNGate";

    public static readonly string[] Hosts =
    [
        "claude.ai",
        "www.claude.ai",
        "api.claude.ai",
        "assets.claude.ai",
        "cdn.claude.ai",
        "claude.com",
        "www.claude.com",
        "platform.claude.com",
        "anthropic.com",
        "www.anthropic.com",
        "api.anthropic.com",
        "console.anthropic.com",
        "statsig.anthropic.com",
        "a-api.anthropic.com",
        "a-cdn.anthropic.com",
    ];

    public static string RenderBlock()
    {
        var lines = new List<string>
        {
            Begin,
            "# Seal applied by Claude VPN Gate. Lifted only when a VPN tunnel owns the default route.",
        };
        foreach (var host in Hosts)
        {
            lines.Add($"127.0.0.1 {host}");
            lines.Add($"::1 {host}");
        }

        lines.Add(End);
        return string.Join('\n', lines) + "\n";
    }

    public static string Upsert(string text, string? block)
    {
        var start = text.IndexOf(Begin, StringComparison.Ordinal);
        while (start >= 0)
        {
            var end = text.IndexOf(End, start, StringComparison.Ordinal);
            if (end < 0)
            {
                text = text[..start];
                break;
            }

            end += End.Length;
            if (end < text.Length && text[end] == '\n')
                end++;
            if (end < text.Length && text[end] == '\r')
                end++;
            text = text[..start] + text[end..];
            start = text.IndexOf(Begin, StringComparison.Ordinal);
        }

        if (block is null)
            return text.TrimEnd() + "\n";

        var body = text.TrimEnd();
        return (body.Length == 0 ? "" : body + "\n\n") + block;
    }
}
