using System.Net;

namespace FiberDistro.Core;

public abstract class Transceiver
{
    public event Action<Packet>? Received;
    
    public IPEndPoint LocalEndPoint = new(0, 0);
    
    public abstract Task SendAsync(Packet packet);

    public Task OnReceived(Packet packet)
    {
        Received?.Invoke(packet);
        return Task.CompletedTask;
    }
}