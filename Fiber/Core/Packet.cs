using System.Buffers;
using System.Net;
using System.Text;

namespace Fiber.Core;

public class Packet
{
    public const int HeaderSize = 37;

    public IPEndPoint Source = new(0, 0);
    
    public IPEndPoint Target = new(0, 0);

    public byte Proto;
    
    public byte[] Payload = [];

    public override string ToString()
    {
        var packet = this;
        return new StringBuilder()
            .Append("[")
            .Append(packet.Proto)
            .Append("] ")
            .Append(packet.Source)
            .Append(" -> ")
            .Append(packet.Target)
            .Append(" payload ")
            .Append(packet.Payload.Length)
            .Append(" bytes")
            .ToString();
    }
    
    public static Packet FromSequence(ref ReadOnlySequence<byte> sequence)
    {
        var length = BitConverter.ToUInt32(sequence.Slice(32, 4).ToArray());
        return new Packet
        {
            Source = Helper.UnserializeIPEndPoint(sequence.Slice(0, 16).ToArray()),
            Target = Helper.UnserializeIPEndPoint(sequence.Slice(16, 16).ToArray()),
            Proto = sequence.Slice(36, 1).FirstSpan[0],
            Payload = sequence.Slice(37, length).ToArray()
        };
    }

    public byte[] ToArray()
    {
        var buffer = new byte[HeaderSize + Payload.Length];
        var source = Source.Serialize().Buffer.ToArray();
        var target = Target.Serialize().Buffer.ToArray();
        Buffer.BlockCopy(source, 0, buffer, 0, source.Length);
        Buffer.BlockCopy(target, 0, buffer, source.Length, target.Length);
        var length = BitConverter.GetBytes((uint) Payload.Length);
        Buffer.BlockCopy(length, 0, buffer, source.Length * 2, 4);
        buffer[HeaderSize - 1] = Proto;
        Buffer.BlockCopy(Payload, 0, buffer, HeaderSize, Payload.Length);
        return buffer;
    }
    
    public static uint ReadPacketSizeFromHeader(ReadOnlySpan<byte> span)
    {
        return BitConverter.ToUInt32(span.Slice(32, 4));
    }
    
    public static uint ReadPacketSizeFromHeaderSequence(ReadOnlySequence<byte> span)
    {
        return BitConverter.ToUInt32(span.Slice(32, 4).ToArray());
    }

    public Packet BuildResponse(byte[] content)
    {
        return new Packet { Proto = ProtoBag.Response, Source = Target, Target = Source, Payload = Payload[..16].Concat(content).ToArray() };
    }

    public byte[] RequestContent => Payload[16..];
}