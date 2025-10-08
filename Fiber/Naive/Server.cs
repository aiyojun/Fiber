using System.Net;
using System.Text;
using Microsoft.Extensions.Hosting;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Host;

namespace Fiber.Naive;

public class Server
{
    private readonly CancellationTokenSource _cancellation = new();
    
    public Server(int port, Func<byte[], string, Task<byte[]>> handle)
    {
        var server = (SuperSocketService<byte[]>) SuperSocketHostBuilder.Create<byte[]?, PipelineFilter>()
            // .UseInProcSessionContainer()
            .UsePackageHandler(async (session, request) =>
            {
                var endpoint = (IPEndPoint) session.RemoteEndPoint;
                await session.Connection.SendAsync(await handle(request!, endpoint.Address + ":" + endpoint.Port));
            })
            .ConfigureSuperSocket(options =>
            {
                options.Name = "Naive Server";
                options.Listeners =
                [
                    new ListenOptions { Ip = "Any", Port = port }
                ];
            })
            .BuildAsServer();
        server.StartAsync(_cancellation.Token);
    }
}