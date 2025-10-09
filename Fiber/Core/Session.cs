using Microsoft.Extensions.Logging;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions.Session;

namespace Fiber.Core;

public class Session : AppSession
{
    public readonly ILogger Logger = LoggerProvider.Logger;
    
    public Server? Wrapper { get; set; }

    public async Task SendAsync(Packet packet)
    {
        Logger.LogDebug("Packet Send : {Packet}", packet.ToString());
        await Connection.SendAsync(packet.ToArray());
    }

    public async Task OnServerReceived(Packet packet)
    {
        var side = Wrapper!;
        if (side.PointToSelf(packet.Target))
        {
            await side.OnReceived(packet);
            return;
        }
        var receiver = packet.Target;
        var session = (Session?) Server.GetSessionContainer().GetSessions().FirstOrDefault(e => receiver.Equals(e.RemoteEndPoint));
        if (session == null)
        {
            Logger.LogDebug("Packet Drop : {}", packet.ToString());
        }
        else
        {
            Logger.LogDebug("PacketRoute : {}", packet.ToString());
            await session.SendAsync(packet);
        }
    }
}