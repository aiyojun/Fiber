using System.Net;
using Microsoft.Extensions.Logging;

namespace FiberDistro.Core;

public class Fiber
{
    public Endpoint Endpoint;
    
    public readonly ILogger Logger = LoggerProvider.Logger;
    
    public const int FiberPort = 9876;

    public Fiber(string masterIp, bool runAsClient = false)
    {
        var masterNetworkPrefix = string.Join(".", masterIp.Split('.')[..3]) + ".";
        var currentIp = Helper.GetNetworkAddress(masterNetworkPrefix);
        Endpoint = masterIp == currentIp && !runAsClient ? new Server(FiberPort) : new Client(masterIp, FiberPort);
        if (Endpoint is Server) Endpoint.IPEndPoint = IPEndPoint.Parse($"{currentIp}:{FiberPort}");
    }
}