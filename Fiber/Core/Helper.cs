using System.Buffers;
using System.Net;

namespace Fiber.Core;

public static class Helper
{
    public static string GetNetworkAddress(string prefix)
    {
        var r = "";
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            var ipAddr = ip.ToString();
            if (!ipAddr.StartsWith(prefix)) continue;
            r = ipAddr;
            break;
        }
        return r;
    }
    
    public static void AssignAddress(byte[] dst, byte[] src)
    {
        Buffer.BlockCopy(src, 0, dst, 0, 8);
    }
    
    public static Packet FromSequence(ref ReadOnlySequence<byte> sequence)
    {
        var packet = new Packet();
        sequence.Slice(0, 8).CopyTo(packet.Source);
        sequence.Slice(8, 8).CopyTo(packet.Target);
        var t = sequence.Slice(16, 4).ToArray();
        var length = BitConverter.ToUInt32(t);
        packet.Proto = sequence.Slice(20, 1).FirstSpan[0];
        packet.Payload = sequence.Slice(21, length).ToArray();
        return packet;
    }

    public static string ToAddress(byte[] address)
    {
        if (address.Length != 8)
            throw new ArgumentException("address length error");
        var span = address.AsSpan();
        return string.Join(".", span[..4].ToArray()) + ":" + BitConverter.ToUInt32(span[4..8]);
    }

    public static byte[] ParseAddress(string address)
    {
        var items = address.Split(':');
        var ip = items[0].Split('.').Select(e => (byte) int.Parse(e)).ToArray();
        var port = int.Parse(items[1]);
        return ip.Concat(BitConverter.GetBytes(port)).ToArray();
    }

    public static byte[] ConvertAddress(byte[] ip, int port)
    {
        return ip.Concat(BitConverter.GetBytes(port)).ToArray();
    }

    public static uint ReadPacketSizeFromHeader(ReadOnlySpan<byte> span)
    {
        return BitConverter.ToUInt32(span.Slice(16, 4));
    }
}