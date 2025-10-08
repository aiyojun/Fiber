using System.Buffers;
using SuperSocket.ProtoBase;

namespace Fiber.Core;

internal class TransportPipelineFilter() : FixedHeaderPipelineFilter<Packet>(21)
{
    protected override int GetBodyLengthFromHeader(ref ReadOnlySequence<byte> buffer)
    {
        return BitConverter.ToInt32(buffer.Slice(16, 4).ToArray());
    }

    protected override Packet DecodePackage(ref ReadOnlySequence<byte> buffer)
    {
        return Helper.FromSequence(ref buffer);
    }
}