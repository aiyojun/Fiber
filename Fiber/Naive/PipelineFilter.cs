using System.Buffers;
using SuperSocket.ProtoBase;

namespace Fiber.Naive;

public class PipelineFilter : PipelineFilterBase<byte[]?>
{
    public override byte[]? Filter(ref SequenceReader<byte> reader)
    {
        if (reader.Remaining == 0) return null;
        var kept = reader.Remaining;
        try
        {
            return reader.Sequence.Slice(reader.Position, kept).ToArray();
        }
        finally
        {
            reader.Advance(kept);    
        }
    }
}