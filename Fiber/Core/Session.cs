using System.Net;
using Microsoft.Extensions.Logging;
using SuperSocket.Connection;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions.Session;

namespace Fiber.Core;

public class Session : AppSession
{
    public readonly ILogger Logger = LoggerProvider.Logger;
    
    public Server? Wrapper { get; set; }
    
    public string? Host { get; set; }

    protected override ValueTask OnSessionConnectedAsync()
    {
        var endpoint = (IPEndPoint) RemoteEndPoint;
        Host = endpoint.Address + ":" + endpoint.Port;
        return ValueTask.CompletedTask;
    }

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
        Logger.LogDebug("PacketRoute : {}", packet.ToString());
        var receiver = Helper.ToAddress(packet.Target);
        var session = (Session?) Server.GetSessionContainer().GetSessions().FirstOrDefault(e => ((Session)e).Host == receiver);
        if (session == null)
        {
            Logger.LogDebug("No such endpoint {}", receiver);
        }
        else
        {
            Logger.LogDebug("FindSession : {1}, state : {4} {5}, Packet receiver : {3}", session.Host, session.State, !session.Connection.IsClosed, receiver);
            await session.SendAsync(packet);
        }
    }
}