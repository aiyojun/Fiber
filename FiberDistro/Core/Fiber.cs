using Microsoft.Extensions.Logging;

namespace FiberDistro.Core;

public class Fiber(Cable cable, int id)
{
    public readonly ILogger Logger = LoggerProvider.Logger;
    
    public int Id { get; set; } = id;

    public event Action<Packet>? Received;
    
    public readonly List<Func<Packet, Task<Packet?>>> Replies = []; 
    
    public int TimeoutMs { get; set; } = 5000;

    private readonly Dictionary<string, TaskCompletionSource<Packet>> _conditions = new();

    public async Task SendAsync(Packet packet)
    {
        await cable.SendAsync(packet);
    }

    public async Task SendMessageAsync(string endpoint, byte[] content)
    {
        await SendAsync(new Packet
        {
            Source = new Location(cable.Sender.LocalEndPoint, Id),
            Target = Location.Parse(endpoint),
            Proto = ProtoBag.Message,
            Payload = content
        });
    }
    
    public async Task<Packet> Request(string endpoint, byte[] content)
    {
        return await Request(new Packet
        {
            Source = new Location(cable.Sender.LocalEndPoint, Id),
            Target = Location.Parse(endpoint),
            Proto = ProtoBag.Request,
            Payload = new byte[16].Concat(content).ToArray()
        });
    }
    
    public async Task<Packet> Request(Packet packet)
    {
        var guid = Guid.NewGuid();
        packet.Proto = ProtoBag.Request;
        packet.Source = new Location(cable.Sender.LocalEndPoint, Id);
        var id = guid.ToByteArray();
        Buffer.BlockCopy(id, 0, packet.Payload, 0, id.Length);
        var condition = new TaskCompletionSource<Packet>(TaskCreationOptions.RunContinuationsAsynchronously);
        var key = guid.ToString("N");
        _conditions[key] = condition;
        await cable.SendAsync(packet);
        var completed = await Task.WhenAny(condition.Task, Task.Delay(TimeSpan.FromMilliseconds(TimeoutMs)));
        _conditions.Remove(key);
        if (completed != condition.Task) throw new TimeoutException();
        return await condition.Task;
    }
    
    public async Task OnReceived(Packet packet)
    {
        switch (packet.Proto)
        {
            case ProtoBag.Message:
                Received?.Invoke(packet);
                break;
            case ProtoBag.Request:
            {
                await cable.SendAsync(await OnRequest(packet));
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
        return new Packet
        {
            Proto = ProtoBag.Response,
            Source = new Location(cable.Sender.LocalEndPoint, Id), 
            Target = packet.Source,
            Payload = payload
        };
    }
}