using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using SuperSocket.Client;

namespace FiberDistro.Core;

public class Client : Endpoint, IDisposable
{
    private readonly IEasyClient<Packet> _client;
    private readonly IPEndPoint _serverAddress;
    private readonly int _interval;
    private bool _disposed;
    private readonly CancellationTokenSource _cancellation;
    
    public readonly ILogger Logger = LoggerProvider.Logger;

    public Client(string ip, int port, int reconnectTimeout = 5)
    {
        _serverAddress = IPEndPoint.Parse($"{ip}:{port}");
        _interval = reconnectTimeout;
        _cancellation = new CancellationTokenSource();
        _client = new EasierClient(new TransportPipelineFilter()).AsClient();
        Task.Run(() => KeepAlive(_cancellation.Token));
    }
    
    ~Client()
    {
        if (_disposed) return;
        Dispose();
    }

    private async Task KeepAlive(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!await _client.ConnectAsync(_serverAddress, _cancellation.Token))
                    throw new Exception();
                var endpoint = (IPEndPoint) ((EasierClient) _client).GetLocalEndPoint();
                IPEndPoint = IPEndPoint.Parse($"{Helper.GetNetworkAddress(string.Join(".", _serverAddress.Address.ToString().Split('.')[..3]) + ".")}:{endpoint.Port}");
                Logger.LogInformation("Run as Client, endpoint : {endpoint}", IPEndPoint.ToString());
                await Work(token);
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to connect fiber master[{ServerIp}], error : {Exception}", _serverAddress.ToString(), e);
            }
            await Task.Delay(TimeSpan.FromSeconds(_interval), token);
        }
    }

    private async Task Work(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var packet = await _client.ReceiveAsync();
            if (packet == null)
                break;
            try
            {
                await OnReceived(packet);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }
        }
    }
    
    public override async Task SendAsync(Packet packet)
    {
        if (PointToSelf(packet.Target))
        {
            await OnReceived(packet);
            return;
        }
        packet.Source = IPEndPoint;
        Logger.LogDebug("Packet Send : {Packet}", packet.ToString());
        await _client.SendAsync(packet.ToArray());
    }

    public void Dispose()
    {
        if (_disposed) return;
        _cancellation.Cancel();
        _client.CloseAsync().AsTask().Wait();
        _client.Dispose();
        _disposed = true;
    }
    
    public async Task<string[]> List()
    {
        return Encoding.UTF8.GetString((await Request(BuildRequest(_serverAddress, "online"u8.ToArray()))).RequestContent).Split(';');
    }
}