// See https://aka.ms/new-console-template for more information

using System.Text;
using Fiber.Core;

var fiber = new Fiber.Core.Fiber("192.168.1.20", true);
var client = (Client) fiber.Endpoint;
Console.WriteLine();
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
        Packet.AssignAddress(packet.Target, Packet.ParseAddress("0.0.0.0:9876"));
        await client.SendAsync(packet);
    }
    else if (x.StartsWith("route#"))
    {
        var items = x.Split("#");
        var packet = new Packet { Proto = Proto.Message, Payload = Encoding.UTF8.GetBytes(items[2]) };
        Packet.AssignAddress(packet.Target, Packet.ParseAddress(items[1]));
        await client.SendAsync(packet);
    }
}