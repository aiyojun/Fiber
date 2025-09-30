namespace Fiber.Core;

public class Proto
{
    public const byte Broadcast = 0x00,
        Message = 0x01,
        Request = 0x02,
        Response = 0x03,
        Authentication = 0x04;

    public static async Task<byte[]> BuildAuthentication(string name)
    {
        return await Write(writer =>
        {
            writer.Write(Authentication);
            writer.Write(name);
        });
    }

    public static async Task<byte[]> BuildResponse(string? name, string receiver, byte[] payload)
    {
        return await Write(writer =>
        {
            writer.Write(Response);
            writer.Write(receiver);
            writer.Write(name ?? "");
            writer.Write(false);
            writer.Write(payload);
        });
    }

    public static async Task<byte[]> BuildError(string? name, string receiver, string error)
    {
        return await Write(writer =>
        {
            writer.Write(Response);
            writer.Write(receiver);
            writer.Write(name ?? "");
            writer.Write(false);
            writer.Write(error);
        });
    }

    public static async Task<byte[]> BuildRequest(string? name, string receiver, Guid guid, string signature,
        byte[] payload)
    {
        return await Write(writer =>
        {
            writer.Write(Request);
            writer.Write(receiver);
            writer.Write(name ?? "");
            writer.Write(guid.ToByteArray());
            writer.Write(signature);
            writer.Write(payload);
        });
    }

    public static async Task<byte[]> BuildMessage(string? name, string receiver, byte[] payload)
    {
        return await Write(writer =>
        {
            writer.Write(Message);
            writer.Write(receiver);
            writer.Write(name ?? "");
            writer.Write(payload);
        });
    }

    private static async Task<byte[]> Write(Action<BinaryWriter> maker)
    {
        using var stream = new MemoryStream();
        await using var writer = new BinaryWriter(stream);
        maker(writer);
        return stream.ToArray();
    }
}