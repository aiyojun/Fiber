using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Session;
using SuperSocket.Server.Host;

namespace FiberDistro.Core;

public class Server : Transceiver, IDisposable
{
    public readonly ILogger Logger = LoggerProvider.Logger;
    
    private readonly SuperSocketService<Packet> _server;
    
    private readonly CancellationTokenSource _cancellation = new();
    
    private bool _disposed;

    public Server(IPEndPoint binding)
    {
        LocalEndPoint = binding;
        _server = (SuperSocketService<Packet>)SuperSocketHostBuilder.Create<Packet, TransportPipelineFilter>()
            .UseSession<Session>()
            .UseInProcSessionContainer()
            .UsePackageHandler(async (session, package) => await OnReceived(package, (Session)session))
            .ConfigureSuperSocket(options =>
            {
                options.Name = "Fiber Server";
                options.Listeners = [ new ListenOptions { Ip = "Any", Port = LocalEndPoint.Port } ];
            })
            .ConfigureLogging((_, builder) => builder.ClearProviders())
            .BuildAsServer();
        _server.StartAsync(_cancellation.Token);
    }

    ~Server()
    {
        if (_disposed) return;
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _cancellation.Cancel();
        _server.StopAsync(_cancellation.Token).Wait();
        _disposed = true;
    }

    public Session[] Sessions => _server.GetSessionContainer().GetSessions().Select(e => (Session)e).ToArray();

    public override async Task SendAsync(Packet packet)
    {
        var session = Sessions.FirstOrDefault(e => packet.Target.BelongsTo((IPEndPoint) e.RemoteEndPoint));
        if (session == null)
        {
            Logger.LogDebug("PacketRoute : route failed, {PacketTarget} offline", packet.Target);
            return;
        }
        await session.SendAsync(packet);
    }

    private async Task OnReceived(Packet packet, Session session)
    {
        if (!packet.Target.BelongsTo(LocalEndPoint))
        {
            await SendAsync(packet);
            return;
        }
        await base.OnReceived(packet);
    }

    public async Task BroadcastAsync(Packet packet)
    {
        foreach (var session in Sessions)
        {
            await session.SendAsync(packet);
        }
    }
}