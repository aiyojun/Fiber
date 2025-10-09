using System.Net;
using Microsoft.Extensions.Logging;

namespace Fiber.Core;

public abstract class Endpoint
{
    public event Action<Packet>? Received;

    public readonly List<Func<Packet, Task<Packet?>>> Replies = [
        packet => Task.FromResult(packet.RequestContent.SequenceEqual("ping"u8.ToArray()) ? packet.BuildResponse("ack"u8.ToArray()) : null)
    ];
    
    public readonly ILogger Logger = LoggerProvider.Logger;
    
    private readonly Dictionary<string, TaskCompletionSource<Packet>> _conditions = new();

    public IPEndPoint IPEndPoint = new(0, 0);
    
    public int TimeoutMs { get; set; } = 5000;

    public bool PointToSelf(IPEndPoint address)
    {
        return IPEndPoint.Equals(address);
    }
    
    public Packet BuildMessage(string receiver, byte[] data)
    {
        return new Packet { Proto = ProtoBag.Message, Source = IPEndPoint, Target = IPEndPoint.Parse(receiver), Payload = data };
    }
    
    public Packet BuildRequest(IPEndPoint receiver, byte[] data)
    {
        var buf = new byte[16 + data.Length];
        Buffer.BlockCopy(data, 0, buf, 16, data.Length);
        return new Packet { Proto = ProtoBag.Request, Source = IPEndPoint, Target = receiver, Payload = buf };
    }
    
    public Packet BuildRequest(string receiver, byte[] data)
    {
        return BuildRequest(IPEndPoint.Parse(receiver), data);
    }

    public abstract Task SendAsync(Packet packet);

    public async Task<Packet> OnRequest(Packet packet)
    {
        byte[] payload = [];
        foreach (var reply in Replies)
        {
            try
            {
                var ret = await reply.Invoke(packet);
                if (ret == null) continue;
                payload = ret.Payload;
                break;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "error while generating response");
            }
        }
        return new Packet { Proto = ProtoBag.Response, Target = packet.Source, Source = IPEndPoint, Payload = payload };
    }
    
    public async Task OnReceived(Packet packet)
    {
        try
        {
            Logger.LogDebug("Packet Recv : {Packet}", packet.ToString());
            switch (packet.Proto)
            {
                case ProtoBag.Message:
                    Received?.Invoke(packet);
                    break;
                case ProtoBag.Request:
                {
                    await SendAsync(await OnRequest(packet));
                    break;
                }
                case ProtoBag.Response:
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
        packet.Proto = ProtoBag.Request;
        packet.Source = IPEndPoint;
        var id = guid.ToByteArray();
        Buffer.BlockCopy(id, 0, packet.Payload, 0, id.Length);
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