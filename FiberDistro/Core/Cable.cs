using System.Net;
using Microsoft.Extensions.Logging;

namespace FiberDistro.Core;

public class Cable
{
    public readonly ILogger Logger = LoggerProvider.Logger;
    
    private readonly Dictionary<int, Fiber> _fibers = new();

    public Transceiver Sender { get; }
    
    public Cable(string master, bool runAsClient = false)
    {
        var currentIp = Helper.GetNetworkAddress(master);
        if (currentIp == master && !runAsClient)
            Sender = new Server(IPEndPoint.Parse($"{master}:9876"));
        else
            Sender = new Client(IPEndPoint.Parse($"{master}:9876"));
        Sender.Received += OnReceived;
        Logger.LogInformation("Run as {Name}, Master : {Master}", Sender.GetType().Name, master);
    }

    public Fiber Create(int id)
    {
        var fiber = new Fiber(this, id);
        Logger.LogDebug("Fiber id: {FiberId}", fiber.Id);
        _fibers.Add(fiber.Id, fiber);
        return fiber;
    }

    public async Task SendAsync(Packet packet)
    {
        var id = packet.Target.Fid;
        if (packet.Target.BelongsTo(Sender.LocalEndPoint) && _fibers.TryGetValue(id, out var fiber))
        {
            await fiber.OnReceived(packet);
        }
        else
        {
            await Sender.SendAsync(packet);
        }
    }

    private void OnReceived(Packet packet)
    {
        var id = packet.Target.Fid;
        if (!_fibers.TryGetValue(id, out var fiber)) return;
        _ = fiber.OnReceived(packet);
    }
}