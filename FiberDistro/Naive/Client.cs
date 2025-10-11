using System.Net;
using FiberDistro.Core;
using Microsoft.Extensions.Logging;
using SuperSocket.Client;

namespace FiberDistro.Naive;

public class Client
{
    public readonly ILogger Logger = LoggerProvider.Logger;

    private readonly IEasyClient<byte[]> _client;

    private readonly int _interval;

    private readonly CancellationTokenSource _cancellation = new();

    private bool _disposed;

    public readonly IPEndPoint RemoteEndPoint;
    
    public event Action<byte[]>? Received; 

    public Client(IPEndPoint serverEndPoint, int reconnectTimeout = 5)
    {
        RemoteEndPoint = serverEndPoint;
        _interval = reconnectTimeout;
        var port = NetworkHelper.FindAvailablePort(serverEndPoint.Port);
        _client = new EasyClient<byte[]>(new PipelineFilter()!).AsClient();
        _client.LocalEndPoint = IPEndPoint.Parse($"0.0.0.0:{port}");
        Logger.LogDebug("Binding port {port}", port);
        _ = KeepAlive(_cancellation.Token);
    }

    ~Client()
    {
        if (_disposed) return;
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _cancellation.Cancel();
        _client.CloseAsync().AsTask().Wait();
        _client.Dispose();
        _disposed = true;
    }
    
    public async Task SendAsync(byte[] packet)
    {
        await _client.SendAsync(packet);
    }
    
    private async Task KeepAlive(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!await _client.ConnectAsync(RemoteEndPoint, _cancellation.Token))
                    throw new Exception();
                // var endpoint = (IPEndPoint)(_client).GetLocalEndPoint();
                // LocalEndPoint =
                    // IPEndPoint.Parse($"{Helper.GetNetworkAddress(RemoteEndPoint.Address.ToString())}:{endpoint.Port}");
                while (!token.IsCancellationRequested)
                {
                    var packet = await _client.ReceiveAsync();
                    if (packet == null)
                        break;
                    try
                    {
                        Received?.Invoke(packet);
                        // await OnReceived(packet);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to connect fiber master[{ServerIp}], error : {Exception}",
                    RemoteEndPoint.ToString(), e);
            }

            await Task.Delay(TimeSpan.FromSeconds(_interval), token);
        }
    }
}