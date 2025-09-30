// See https://aka.ms/new-console-template for more information

using System.Text;
using Fiber.Core;

var fiber = new Fiber.Core.Fiber("192.168.1.20");
Console.WriteLine();
while (true)
{
    Console.Write("> ");
    var x = Console.ReadLine();
    if (x == null) continue;
    if (x.StartsWith("exit"))
    {
        break;
    }

    if (x.StartsWith("message#"))
    {
        var server = (Server)fiber.Endpoint;
        var packet = new Packet { Proto = Proto.Message, Payload = Encoding.UTF8.GetBytes(x[8..]) };
        Console.WriteLine($"- Connections : {server.Sessions.Values.ToArray().Length}");
        Packet.AssignAddress(packet.Source, Packet.ParseAddress("0.0.0.0:9876"));
        foreach (var session in server.Sessions.Values.ToArray())
        {
            Packet.AssignAddress(packet.Target, Packet.ParseAddress(session.Host!));
            await fiber.Endpoint.SendAsync(packet);
        }
    }
}