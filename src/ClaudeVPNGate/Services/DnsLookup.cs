using System.Net;
using System.Net.Sockets;

namespace ClaudeVPNGate.Services;

public static class DnsLookup
{
    public static readonly string[] ResolveHosts =
    [
        "claude.ai",
        "www.claude.ai",
        "api.anthropic.com",
        "console.anthropic.com",
        "platform.claude.com",
    ];

    public static int Ipv4Dword(string ip)
        => BitConverter.ToInt32(IPAddress.Parse(ip).GetAddressBytes(), 0);

    public static List<string> ParseARecords(byte[] packet)
    {
        var found = new List<string>();
        if (packet.Length < 12)
            return found;

        var questions = ReadU16(packet, 4);
        var answers = ReadU16(packet, 6);
        var cursor = 12;
        for (var i = 0; i < questions; i++)
        {
            cursor = SkipName(packet, cursor);
            cursor += 4;
        }

        for (var i = 0; i < answers; i++)
        {
            cursor = SkipName(packet, cursor);
            if (cursor + 10 > packet.Length)
                break;
            var type = ReadU16(packet, cursor);
            var length = ReadU16(packet, cursor + 8);
            cursor += 10;
            if (type == 1 && length == 4 && cursor + 4 <= packet.Length)
                found.Add(new IPAddress(packet.AsSpan(cursor, 4)).ToString());
            cursor += length;
        }

        return found;
    }

    public static HashSet<string> ResolveClaudeIpv4()
    {
        var found = new HashSet<string>(StringComparer.Ordinal);
        foreach (var host in ResolveHosts)
        {
            foreach (var server in new[] { "1.1.1.1", "8.8.8.8" })
            {
                var answers = Query(host, server);
                if (answers.Count == 0)
                    continue;
                foreach (var ip in answers)
                {
                    if (!ip.StartsWith("127.", StringComparison.Ordinal))
                        found.Add(ip);
                }

                break;
            }
        }

        return found;
    }

    private static List<string> Query(string host, string server)
    {
        var id = Random.Shared.Next(1, ushort.MaxValue);
        var packet = new List<byte>
        {
            (byte)(id >> 8), (byte)id,
            0x01, 0x00,
            0x00, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };
        foreach (var label in host.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(label);
            packet.Add((byte)bytes.Length);
            packet.AddRange(bytes);
        }

        packet.AddRange([0x00, 0x00, 0x01, 0x00, 0x01]);
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.ReceiveTimeout = 1200;
            socket.SendTimeout = 1200;
            socket.SendTo(packet.ToArray(), new IPEndPoint(IPAddress.Parse(server), 53));
            var buffer = new byte[2048];
            var read = socket.Receive(buffer);
            return ParseARecords(buffer.AsSpan(0, read).ToArray());
        }
        catch (SocketException)
        {
            return [];
        }
    }

    private static int ReadU16(byte[] packet, int offset)
        => (packet[offset] << 8) | packet[offset + 1];

    private static int SkipName(byte[] packet, int cursor)
    {
        var hops = 0;
        while (cursor < packet.Length && hops++ < 40)
        {
            var length = packet[cursor];
            if (length == 0)
                return cursor + 1;
            if ((length & 0xC0) == 0xC0)
                return cursor + 2;
            cursor += 1 + length;
        }

        return packet.Length;
    }
}
