using System.Net;
using System.Net.NetworkInformation;

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
    
    /// <summary>
    /// 从指定起始端口开始，返回第一个可用（未被占用）的端口号。
    /// </summary>
    /// <param name="startPort">起始端口号（例如 8000）</param>
    /// <param name="maxPort">最大扫描端口号（默认 65535）</param>
    /// <returns>找到的可用端口号</returns>
    /// <exception cref="InvalidOperationException">未找到可用端口</exception>
    public static int FindAvailablePort(int startPort, int maxPort = 65535)
    {
        if (startPort is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(startPort), "Port number must in range of 1~65535");

        var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
        var usedPorts = ipProperties.GetActiveTcpListeners()
            .Select(p => p.Port)
            .ToHashSet();

        for (var port = startPort; port <= maxPort; port++)
        {
            if (!usedPorts.Contains(port))
                return port;
        }

        throw new InvalidOperationException($"No available port from {startPort}");
    }
}