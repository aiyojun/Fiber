using System.Buffers;

namespace Fiber.Core;

public class Packet
{
    public readonly byte[] Source = new byte[8];
    
    public readonly byte[] Target = new byte[8];

    public byte Proto;
    
    public byte[] Payload = [];

    public byte[] ToArray()
    {
        var buffer = new byte[21 + Payload.Length];
        Buffer.BlockCopy(Source, 0, buffer, 0, Source.Length);
        Buffer.BlockCopy(Target, 0, buffer, 8, Target.Length);
        var length = BitConverter.GetBytes((uint) Payload.Length);
        Buffer.BlockCopy(length, 0, buffer, 16, 4);
        buffer[20] = Proto;
        Buffer.BlockCopy(Payload, 0, buffer, 21, Payload.Length);
        return buffer;
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