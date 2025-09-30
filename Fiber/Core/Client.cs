using System.Buffers;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Fiber.Core;

public class Client : Endpoint, IDisposable
{
    private readonly string _serverIp;
    private readonly int _serverPort;
    private readonly int _interval;
    private readonly CancellationTokenSource? _cancellation;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private Task? _checkTask;
    private bool _disposed;

    public readonly ILogger Logger = FiberLogger.Logger;
    
    public Client(string ip, int port, int reconnectTimeout = 5)
    {
        _serverIp = ip;
        _serverPort = port;
        _interval = reconnectTimeout;
        _cancellation = new CancellationTokenSource();
        _checkTask = Task.Run(() => KeepAlive(_cancellation.Token));
    }

    ~Client()
    {
        if (_disposed) return;
        Dispose();
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _cancellation?.Cancel();
        _client?.Close();
        _stream?.Dispose();
        _disposed = true;
    }

    public async Task SendAsync(byte[] payload)
    {
        if (_stream == null || !_client!.Connected) 
            throw new Exception("Disconnected from server side");
        await _stream.WriteAsync(payload);
    }

    public async Task OnClientReceived(byte[] buffer)
    {
        var sequence = new ReadOnlySequence<byte>(buffer);
        var packet = Packet.FromSequence(ref sequence);
        await OnReceived(packet);
    }
    
    private async Task KeepAlive(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (_client is not { Connected: true })
            {
                try
                {
                    _client = new TcpClient();
                    await _client.ConnectAsync(_serverIp, _serverPort, token);
                    var endpoint = (_client.Client.LocalEndPoint as IPEndPoint)!;
                    Ip = Helper.GetNetworkAddress(string.Join(".", _serverIp.Split('.')[..3]) + ".").Split('.').Select(e => (byte) int.Parse(e)).ToArray();
                    Port = endpoint.Port;
                    _stream = _client.GetStream();
                    Logger.LogInformation("Server side connected");
                    _ = Task.Run(() => Work(_stream, token), token);
                }
                catch (Exception e)
                {
                    Logger.LogError("Failed to connect fiber master[{ServerIp}:{ServerPort}], error : {Exception}", _serverIp, _serverPort, e);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(_interval), token);
        }
    }

    private async Task Work(NetworkStream stream, CancellationToken token)
    {
        var header = new byte[21];
        try
        {
            while (!token.IsCancellationRequested && _client!.Connected)
            {
                var read = await stream.ReadAsync(header.AsMemory(0, 21), token);
                if (read == 0) break; // disconnect
                var length = Packet.ReadPacketSizeFromHeader(header);
                if (length > int.MaxValue) throw new Exception("Packet size too large");
                var buffer = new byte[length];
                var received = 0;
                while (received < length)
                {
                    var n = await stream.ReadAsync(buffer.AsMemory(received, (int) length - received), token);
                    if (n == 0) break;
                    received += n;
                }
                await OnClientReceived(header.Concat(buffer).ToArray());
            }
        }
        catch (Exception e)
        {
            Logger.LogError("{Exception}", e);
        }
    }

    public override async Task SendAsync(Packet packet)
    {
        var addr = Ip.Concat(BitConverter.GetBytes(Port)).ToArray();
        Buffer.BlockCopy(addr, 0, packet.Source, 0, 8);
        await SendAsync(packet.ToArray());
    }

    public override Task OnMessage(byte[] data)
    {
        Logger.LogInformation("Client::OnMessage : {GetString}", Encoding.UTF8.GetString(data));
        return Task.CompletedTask;
    }

    public override Task<byte[]> OnRequest(byte[] data)
    {
        throw new NotImplementedException();
    }
}
