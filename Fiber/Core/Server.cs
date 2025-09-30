using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Host;

namespace Fiber.Core;

public class Server : Endpoint, IDisposable
{
    public readonly ILogger Logger = FiberLogger.Logger;
    
    public readonly Dictionary<string, FiberSession> Sessions = [];

    private readonly IHost _server;

    private readonly Task _worker;
    
    private bool _disposed;
    
    public Server(int port)
    {
        Ip = "\0\0\0\0"u8.ToArray();
        Port = port;
        _server = SuperSocketHostBuilder.Create<Packet, TransportPipelineFilter>()
            .UseSession<FiberSession>()
            .UsePackageHandler(async (session, package) => await ((FiberSession) session).OnServerReceived(package))
            .UseSessionHandler(onConnected: session =>
            {
                var fiberSession = (session as FiberSession)!;
                Sessions.Add(fiberSession.Host!, fiberSession);
                fiberSession.Server = this;
                Logger.LogInformation("Fiber endpoint {FiberSessionHost} online", fiberSession.Host);
                return ValueTask.CompletedTask;
            }, onClosed: (session, _) =>
            {
                var fiberSession = (session as FiberSession)!;
                Sessions.Remove(fiberSession.Host!);
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
            .Build();
        _worker = _server.RunAsync();
    }
    
    
    public void Dispose()
    {
        if (_disposed) return;
        _server.StopAsync().Wait();
        _worker.Wait();
        _server.Dispose();
        _disposed = true;
    }

    public override async Task SendAsync(Packet packet)
    {
        var destination = Packet.ToAddress(packet.Target);
        if (!Sessions.TryGetValue(destination, out var session))
            throw new Exception($"{destination} offline");
        await session.SendAsync(packet);
    }

    public override Task OnMessage(byte[] data)
    {
        Logger.LogDebug("Server::OnMessage {GetString}", Encoding.UTF8.GetString(data));
        return Task.CompletedTask;
    }

    public override Task<byte[]> OnRequest(byte[] data)
    {
        throw new NotImplementedException();
    }
}