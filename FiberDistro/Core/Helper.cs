using System.Net;
using System.Net.Sockets;

namespace FiberDistro.Core;

public static class Helper
{
    public static IPEndPoint UnserializeIPEndPoint(ReadOnlySpan<byte> buf)
    {
        var socketAddress = new SocketAddress(buf.Length == 16 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6, buf.Length);
        for (var i = 0; i < buf.Length; i++) { socketAddress[i] = buf[i]; }
        return (IPEndPoint) new IPEndPoint(0, 0).Create(socketAddress);
    }
    
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