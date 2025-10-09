namespace Fiber.Core;

public class ProtoBag
{
    public const byte Broadcast = 0x00,
        Message = 0x01,
        Request = 0x02,
        Response = 0x03,
        Authentication = 0x04;
}