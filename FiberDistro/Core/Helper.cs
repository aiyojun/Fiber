using System.Net;

namespace FiberDistro.Core;

public static class Helper
{
    public static string GetNetworkAddress(string network)
    {
        if (network[^1] != '.') 
            network = string.Join(".", network.Split('.')[..3]) + ".";
        var r = "";
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            var ipAddr = ip.ToString();
            if (!ipAddr.StartsWith(network)) continue;
            r = ipAddr;
            break;
        }
        return r;
    }
}