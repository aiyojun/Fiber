using System.Net;
using Microsoft.Extensions.Logging;

namespace FiberDistro.Core;

public class Cable
{
    public readonly ILogger Logger = LoggerProvider.Logger;

    private Cable()
    {
        var currentIp = Helper.GetNetworkAddress(Master);
        if (currentIp == Master && !RunAsClient)
            Sender = new Server(IPEndPoint.Parse($"{Master}:9876"));
        else
            Sender = new Client(IPEndPoint.Parse($"{Master}:9876"));
        Sender.Received += OnReceived;
    }

    public static string Master = "10.1.16.37";

    public static bool RunAsClient = false;

    private static Cable? _instance;

    public static Cable GetInstance()
    {
        _instance ??= new Cable();
        return _instance;
    }

    private readonly Dictionary<int, Fiber> _fibers = new();

    public Transceiver Sender { get; }

    public void Join(Fiber fiber)
    {
        _fibers.Add(fiber.Id, fiber);
    }

    public int Count()
    {
        return _fibers.Count;
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