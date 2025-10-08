using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Session;
using SuperSocket.Server.Host;

namespace Fiber.Core;

public class Server : Endpoint, IDisposable
{
    public readonly ILogger Logger = LoggerProvider.Logger;
    
    private readonly SuperSocketService<Packet> _server;

    private readonly CancellationTokenSource _token = new();
    
    private bool _disposed;
    
    public Server(int port)
    {
        Ip = "\0\0\0\0"u8.ToArray();
        Port = port;
        _server = (SuperSocketService<Packet>) SuperSocketHostBuilder.Create<Packet, TransportPipelineFilter>()
            .UseSession<Session>()
            .UseInProcSessionContainer()
            .UsePackageHandler(async (session, package) => await ((Session) session).OnServerReceived(package))
            .UseSessionHandler(onConnected: session =>
            {
                var fiberSession = (session as Session)!;
                fiberSession.Wrapper = this;
                return ValueTask.CompletedTask;
            }, onClosed: (session, _) =>
            {
                var fiberSession = (session as Session)!;
                return ValueTask.CompletedTask;
            })
            .ConfigureSuperSocket(options =>
            {
                options.Name = "Fiber Server";
                options.Listeners =
                [
                    new ListenOptions { Ip = "Any", Port = Port }
                ];
            })
            .ConfigureLogging((_, builder) => builder.ClearProviders())
            .BuildAsServer();
        _server.StartAsync(_token.Token);
    }
    
    
    public void Dispose()
    {
        if (_disposed) return;
        _server.StopAsync(_token.Token).Wait();
        _disposed = true;
    }

    public override async Task SendAsync(Packet packet)
    {
        var receiver = Helper.ToAddress(packet.Target);
        var server = this;
        if (server.PointToSelf(packet.Target))
        {
            Logger.LogDebug("Directly arrive");
            await server.OnReceived(packet);
            return;
        }
        var session = GetSessions().FirstOrDefault(e => e.Host == receiver);
        if (session == null)
        {
            Logger.LogDebug("Not found session, receiver : {}", receiver);
            throw new Exception($"{receiver} offline.");
        }
        Logger.LogDebug("FindSession : {1}, Packet receiver : {3}", session.Host, receiver);
        Buffer.BlockCopy(Ip.Concat(BitConverter.GetBytes(Port)).ToArray(), 0, packet.Source, 0, 8);
        await session.SendAsync(packet);
    }

    public override Task OnMessage(byte[] data)
    {
        Logger.LogDebug("Server::OnMessage {GetString}", Encoding.UTF8.GetString(data));
        return Task.CompletedTask;
    }

    public override Task<byte[]> OnRequest(byte[] data)
    {
        if (data.SequenceEqual("online"u8.ToArray()))
            return Task.FromResult(Encoding.UTF8.GetBytes(string.Join(";", List())));
        return base.OnRequest(data);
    }

    public Session[] GetSessions()
    {
        return _server.GetSessionContainer().GetSessions().Select(e => (Session) e).ToArray();
    }
    
    public string[] List()
    {
        var r = GetSessions().Select(e => e.Host!).ToList();
        r.Add(Helper.ToAddress(Ip.Concat(BitConverter.GetBytes(Port)).ToArray()));
        return r.ToArray();
    }
}