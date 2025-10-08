using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using SuperSocket.Client;

namespace Fiber.Core;

public class Client : Endpoint, IDisposable
{
    private readonly IEasyClient<Packet> _client;
    private readonly string _serverIp;
    private readonly int _serverPort;
    private readonly int _interval;
    private bool _disposed;
    private readonly CancellationTokenSource _cancellation;
    
    public readonly ILogger Logger = LoggerProvider.Logger;

    public Client(string ip, int port, int reconnectTimeout = 5)
    {
        _serverIp = ip;
        _serverPort = port;
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
                if (!await _client.ConnectAsync(new IPEndPoint(IPAddress.Parse(_serverIp), _serverPort), _cancellation.Token))
                    throw new Exception();
                var endpoint = (IPEndPoint) ((EasierClient) _client).GetLocalEndPoint();
                // Console.WriteLine($"endpoint : {endpoint} {_client.GetType().Name}");
                Ip = Helper.GetNetworkAddress(string.Join(".", _serverIp.Split('.')[..3]) + ".").Split('.').Select(e => (byte) int.Parse(e)).ToArray();
                Port = endpoint.Port;
                Logger.LogInformation("Server side connected");
                await Work(token);
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to connect fiber master[{ServerIp}:{ServerPort}], error : {Exception}", _serverIp, _serverPort, e);
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
        Buffer.BlockCopy(Ip.Concat(BitConverter.GetBytes(Port)).ToArray(), 0, packet.Source, 0, 8);
        Logger.LogDebug("Packet Send : {Packet}", packet.ToString());
        await _client.SendAsync(packet.ToArray());
    }

    public override Task OnMessage(byte[] data)
    {
        Logger.LogInformation("Client::OnMessage : {GetString}", Encoding.UTF8.GetString(data));
        return Task.CompletedTask;
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
        var packet = new Packet { Proto = Proto.Request, Payload = "online"u8.ToArray() };
        Helper.AssignAddress(packet.Target, Helper.ParseAddress(_serverIp + ":" + _serverPort));
        return Encoding.UTF8.GetString((await Request(packet)).Payload[16..]).Split(';');
    }
}