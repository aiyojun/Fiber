using System.Net;

namespace Fiber.Core;

public static class Helper
{
    public static string GetNetworkAddress(string prefix)
    {
        var r = "";
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            var ipAddr = ip.ToString();
            if (!ipAddr.StartsWith(prefix)) continue;
            r = ipAddr;
            break;
        }
        return r;
    }
}