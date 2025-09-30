using System.Net;
using Microsoft.Extensions.Logging;
using SuperSocket.Server;

namespace Fiber.Core;

public class FiberSession : AppSession
{
    public readonly ILogger Logger = FiberLogger.Logger;
    
    public Server? Server { get; set; }
    
    public string? Host { get; set; }

    protected override ValueTask OnSessionConnectedAsync()
    {
        var endpoint = (IPEndPoint) RemoteEndPoint;
        Host = endpoint.Address + ":" + endpoint.Port;
        return ValueTask.CompletedTask;
    }

    public async Task SendAsync(Packet packet)
    {
        await Connection.SendAsync(packet.ToArray());
    }

    public async Task OnServerReceived(Packet packet)
    {
        Logger.LogDebug("Server::OnServerReceived : {ToString}, Source : {ToAddress}, Target : {S}", BitConverter.ToString(packet.ToArray()), Packet.ToAddress(packet.Source), Packet.ToAddress(packet.Target));
        var receiver = Packet.ToAddress(packet.Target);
        if (receiver == $"0.0.0.0:{Server!.Port}")
            await Server!.OnReceived(packet);
        else
            await Server!.SendAsync(packet);
    }
}