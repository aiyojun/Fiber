// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Text;
using Fiber.Core;

var fiber = new Fiber.Core.Fiber("192.168.1.20", true);
var client = (Client) fiber.Endpoint;
Console.WriteLine();
var master = "0.0.0.0:9876";
while (true)
{
    var x = Console.ReadLine();
    if (x == null) continue;
    if (x.StartsWith("exit"))
    {
        break;
    }
    if (x.StartsWith("message#"))
    {
        Console.WriteLine($"- Client : {x[8..]}");
        var packet = new Packet { Proto = Proto.Message, Payload = Encoding.UTF8.GetBytes(x[8..]) };
        Packet.AssignAddress(packet.Target, Packet.ParseAddress(master));
        await client.SendAsync(packet);
    }
    else if (x.StartsWith("route#"))
    {
        var items = x.Split("#");
        var packet = new Packet { Proto = Proto.Message, Payload = Encoding.UTF8.GetBytes(items[2]) };
        Packet.AssignAddress(packet.Target, Packet.ParseAddress(items[1]));
        await client.SendAsync(packet);
    }
    else if (x == "ping")
    {
        var packet = new Packet { Proto = Proto.Request, Payload = "ping"u8.ToArray() };
        Packet.AssignAddress(packet.Target, Packet.ParseAddress(master));
        var sw = Stopwatch.StartNew();
        var resp = await client.Request(packet);
        sw.Stop();
        Console.WriteLine($"- Cost : {sw.ElapsedMilliseconds} ms, Response : " + Encoding.UTF8.GetString(resp.Payload[16..]));
    }
}