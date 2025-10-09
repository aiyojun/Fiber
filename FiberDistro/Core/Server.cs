using System.Net;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Session;
using SuperSocket.Server.Host;

namespace FiberDistro.Core;

public class Server : Endpoint, IDisposable
{
    public readonly ILogger Logger = LoggerProvider.Logger;
    
    private readonly SuperSocketService<Packet> _server;

    private readonly CancellationTokenSource _token = new();
    
    private bool _disposed;
    
    public Server(int port)
    {
        IPEndPoint = new IPEndPoint(0, port);
        Replies.Add(packet => Task.FromResult(packet.RequestContent.SequenceEqual("online"u8.ToArray()) ? packet.BuildResponse(Encoding.UTF8.GetBytes(string.Join(";", List()))) : null));
        _server = (SuperSocketService<Packet>) SuperSocketHostBuilder.Create<Packet, TransportPipelineFilter>()
            .UseSession<Session>()
            .UseInProcSessionContainer()
            .UsePackageHandler(async (session, package) => await ((Session) session).OnServerReceived(package))
            .UseSessionHandler(onConnected: session =>
            {
                var fiberSession = (session as Session)!;
                fiberSession.Wrapper = this;
                return ValueTask.CompletedTask;
            }, onClosed: (_, _) => ValueTask.CompletedTask)
            .ConfigureSuperSocket(options =>
            {
                options.Name = "Fiber Server";
                options.Listeners =
                [
                    new ListenOptions { Ip = "Any", Port = IPEndPoint.Port }
                ];
            })
            .ConfigureLogging((_, builder) => builder.ClearProviders())
            .BuildAsServer();
        _server.StartAsync(_token.Token);
        Logger.LogInformation("Run as Server, endpoint : {endpoint}", IPEndPoint.ToString());
    }
    
    
    public void Dispose()
    {
        if (_disposed) return;
        _server.StopAsync(_token.Token).Wait();
        _disposed = true;
    }

    public override async Task SendAsync(Packet packet)
    {
        var receiver = packet.Target;
        var server = this;
        if (server.PointToSelf(packet.Target))
        {
            Logger.LogDebug("Directly arrive");
            await server.OnReceived(packet);
            return;
        }
        var session = GetSessions().FirstOrDefault(e => receiver.Equals(e.RemoteEndPoint));
        if (session == null)
        {
            Logger.LogDebug("Not found session, receiver : {}", receiver.ToString());
            throw new Exception($"{receiver} offline.");
        }
        // Logger.LogDebug("FindSession : {1}, Packet receiver : {3}", session.RemoteEndPoint.ToString(), receiver.ToString());
        packet.Source = IPEndPoint;
        await session.SendAsync(packet);
    }

    public Session[] GetSessions()
    {
        return _server.GetSessionContainer().GetSessions().Select(e => (Session) e).ToArray();
    }
    
    public string[] List()
    {
        var r = GetSessions().Select(e => e.RemoteEndPoint.ToString()!).ToList();
        r.Add(IPEndPoint.ToString());
        return r.ToArray();
    }
}