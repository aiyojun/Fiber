using Microsoft.Extensions.Logging;

namespace Fiber.Core;

public abstract class Endpoint
{
    public readonly ILogger Logger = LoggerProvider.Logger;
    
    private readonly Dictionary<string, TaskCompletionSource<Packet>> _conditions = new();

    public byte[] Ip = new byte[4];

    public int Port = 9876;

    public int TimeoutMs { get; set; } = 5000;

    public bool PointToSelf(byte[] address)
    {
        return Ip.Concat(BitConverter.GetBytes(Port)).ToArray().SequenceEqual(address);
    }

    public abstract Task SendAsync(Packet packet);

    public abstract Task OnMessage(byte[] data);

    public virtual Task<byte[]> OnRequest(byte[] data)
    {
        return Task.FromResult(!data.SequenceEqual("ping"u8.ToArray()) ? [] : "ack"u8.ToArray());
    }
    
    public async Task OnReceived(Packet packet)
    {
        try
        {
            Logger.LogDebug("Packet Recv : {Packet}", packet.ToString());
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
                    reply.Payload = packet.Payload[..16].Concat(await OnRequest(packet.Payload[16..])).ToArray();
                    await SendAsync(reply);
                    break;
                }
                case Proto.Response:
                {
                    if (!_conditions.TryGetValue(new Guid(packet.Payload[..16]).ToString("N"), out var condition))
                    {
                        return;
                    }
                    condition.SetResult(packet);
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e.ToString());
        }
        
    }

    public async Task<Packet> Request(Packet packet)
    {
        var guid = Guid.NewGuid();
        packet.Proto = Proto.Request;
        Buffer.BlockCopy(Ip.Concat(BitConverter.GetBytes(Port)).ToArray(), 0, packet.Source, 0, 8);
        packet.Payload = guid.ToByteArray().Concat(packet.Payload).ToArray();
        var condition = new TaskCompletionSource<Packet>(TaskCreationOptions.RunContinuationsAsynchronously);
        var key = guid.ToString("N");
        _conditions[key] = condition;
        await SendAsync(packet);
        var completed = await Task.WhenAny(condition.Task, Task.Delay(TimeSpan.FromMilliseconds(TimeoutMs)));
        _conditions.Remove(key);
        if (completed != condition.Task) throw new TimeoutException();
        var r = await condition.Task;
        return r;
    }
}