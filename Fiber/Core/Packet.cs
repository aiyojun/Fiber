using System.Buffers;
using System.Text;

namespace Fiber.Core;

public class Packet
{
    public readonly byte[] Source = new byte[8];
    
    public readonly byte[] Target = new byte[8];

    public byte Proto;
    
    public byte[] Payload = [];

    public override string ToString()
    {
        var packet = this;
        return new StringBuilder()
            .Append("Proto[")
            .Append(packet.Proto)
            .Append("] ")
            .Append(Helper.ToAddress(packet.Source))
            .Append(" -> ")
            .Append(Helper.ToAddress(packet.Target))
            .Append(" (Payload ")
            .Append(packet.Payload.Length)
            .Append(" bytes)")
            // .Append("---\n")
            // .Append($"  Packet : {BitConverter.ToString(packet.ToArray())}\n")
            // .Append($"  Source : {Packet.ToAddress(packet.Source)}\n")
            // .Append($"  Target : {Packet.ToAddress(packet.Target)}\n")
            // .Append($"   Proto : {packet.Proto}\n")
            // .Append($" Payload : {BitConverter.ToString(packet.Payload)}\n")
            // .Append("---")
            .ToString();
    }

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
}