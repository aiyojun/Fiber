using SuperSocket.Server;

namespace FiberDistro.Core;

public class Session : AppSession
{
    public async Task SendAsync(Packet packet)
    {
        await Connection.SendAsync(packet.ToArray());
    }
}