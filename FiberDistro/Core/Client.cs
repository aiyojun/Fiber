using System.Net;
using Microsoft.Extensions.Logging;
using SuperSocket.Client;

namespace FiberDistro.Core;

public class Client : Transceiver, IDisposable
{
    public readonly ILogger Logger = LoggerProvider.Logger;

    private readonly IEasyClient<Packet> _client;

    private readonly int _interval;

    private readonly CancellationTokenSource _cancellation = new();

    private bool _disposed;

    public readonly IPEndPoint RemoteEndPoint;

    public Client(IPEndPoint serverEndPoint, int reconnectTimeout = 5)
    {
        RemoteEndPoint = serverEndPoint;
        _interval = reconnectTimeout;
        _client = new EasierClient(new TransportPipelineFilter()).AsClient();
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

    private async Task KeepAlive(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!await _client.ConnectAsync(RemoteEndPoint, _cancellation.Token))
                    throw new Exception();
                var endpoint = (IPEndPoint)((EasierClient)_client).GetLocalEndPoint();
                LocalEndPoint =
                    IPEndPoint.Parse($"{Helper.GetNetworkAddress(RemoteEndPoint.Address.ToString())}:{endpoint.Port}");
                await Work(token);
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to connect fiber master[{ServerIp}], error : {Exception}",
                    RemoteEndPoint.ToString(), e);
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
        await _client.SendAsync(packet.ToArray());
    }

    public new async Task OnReceived(Packet packet)
    {
        if (!packet.Target.BelongsTo(LocalEndPoint))
            return;
        await base.OnReceived(packet);
    }
}