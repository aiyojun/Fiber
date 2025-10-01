using Microsoft.Extensions.Logging;

namespace Fiber.Core;

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
        Logger.LogInformation("Run as {Name}", Endpoint.GetType().Name);
    }
}