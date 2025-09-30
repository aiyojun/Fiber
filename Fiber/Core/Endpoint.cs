using System.Text;
using Microsoft.Extensions.Logging;

namespace Fiber.Core;

public abstract class Endpoint
{
    public readonly ILogger Logger = FiberLogger.Logger;
    
    private readonly Dictionary<string, TaskCompletionSource<byte[]>> _conditions = new();

    public byte[] Ip = new byte[4];

    public int Port = 9876;

    public int TimeoutMs { get; set; } = 5000;

    public abstract Task SendAsync(Packet packet);

    public abstract Task OnMessage(byte[] data);

    public abstract Task<byte[]> OnRequest(byte[] data);

    public async Task OnReceived(Packet packet)
    {
        Logger.LogDebug(new StringBuilder("---")
            .Append($"  Packet : {BitConverter.ToString(packet.ToArray())}")
            .Append($"  Source : {Packet.ToAddress(packet.Source)}")
            .Append($"  Target : {Packet.ToAddress(packet.Target)}")
            .Append($"   Proto : {packet.Proto}")
            .Append("---").ToString());
        switch (packet.Proto)
        {
            case Proto.Message:
                await OnMessage(packet.Payload);
                break;
            case Proto.Request:
            {
                var reply = new Packet { Proto = Proto.Response };
                Buffer.BlockCopy(packet.Source, 0, reply.Target, 0, 8);
                Buffer.BlockCopy(packet.Target, 0, reply.Source, 0, 8);
                reply.Payload = await OnRequest(packet.Payload);
                await SendAsync(reply);
                break;
            }
            case Proto.Response:
            {
                var uuidBytes = packet.Payload[..16];
                if (!_conditions.TryGetValue(new Guid(uuidBytes).ToString("N"), out var condition)) return;
                condition.SetResult(packet.Payload);
                break;
            }
        }
    }

    public async Task<byte[]> Request(Endpoint receiver, byte[] data)
    {
        var guid = Guid.NewGuid();
        var packet = new Packet { Proto = Proto.Request, Payload = data };
        Buffer.BlockCopy(Ip, 0, packet.Source, 0, 8);
        Buffer.BlockCopy(receiver.Ip, 0, packet.Target, 0, 8);
        await SendAsync(packet);
        var condition = new TaskCompletionSource<byte[]>();
        var key = guid.ToString("N");
        _conditions[key] = condition;
        var completed = await Task.WhenAny(condition.Task, Task.Delay(TimeSpan.FromMilliseconds(TimeoutMs)));
        _conditions.Remove(key);
        if (completed != condition.Task) throw new TimeoutException();
        var resp = await condition.Task;
        using var stream = new MemoryStream(resp, 16, resp.Length - 16);
        using var reader = new BinaryReader(stream);
        return reader.ReadBoolean()
            ? throw new Exception(reader.ReadString())
            : reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
    }
}