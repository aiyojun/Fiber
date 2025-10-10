using System.Diagnostics;
using System.Net;
using System.Text;
using FiberDistro.Core;

if (args.Length == 0) throw new Exception("Usage : fiber master_ip");
var masterIp = args[0];
var runAsClient = args.Length > 1 && args[1] == "cli";


Cable.Master = masterIp;
Cable.RunAsClient = runAsClient;
var cable = Cable.GetInstance();

var fiber = new Fiber(cable);
fiber.Replies.Add(packet =>
{
    Console.WriteLine($" > {packet.RequestContent.SequenceEqual("ping"u8.ToArray())}");
    return Task.FromResult(packet.RequestContent.SequenceEqual("ping"u8.ToArray())
        ? packet.BuildResponse("ack"u8.ToArray())
        : null);
});
// var endpoint = fiber.Endpoint;
// endpoint.Received += packet => Console.WriteLine("[MESSAGE] " + Encoding.UTF8.GetString(packet.Payload)); 
fiber.Received += packet => Console.WriteLine("[MESSAGE] " + Encoding.UTF8.GetString(packet.Payload)); 
Thread.Sleep(500);
// Console.WriteLine();
// Console.Write("> ");
while (true)
{
    var x = Console.ReadLine();
    if (x == null) break;
    x = x.Trim();
    try
    {
        if (x.Equals("> exit")) break;
        // if (x.Equals("> list"))
        // {
        //     var hosts = endpoint is Server server ? server.List() : await ((Client) endpoint).List();
        //     Console.WriteLine("Online endpoints :\n---");
        //     foreach (var host in hosts)
        //     {
        //         Console.WriteLine($"  {host}");
        //     }
        //     Console.WriteLine("---");
        // }
        if (x.StartsWith("> ping "))
        {
            var targetHost = x[7..].Trim();
            // var packet = fiber.Request(targetHost, "ping"u8.ToArray());
            try
            {
                var sw = Stopwatch.StartNew();
                // var resp = await fiber.Request(packet);
                var resp = await fiber.Request(targetHost, "ping"u8.ToArray());
                sw.Stop();
                Console.WriteLine($"{targetHost} Reply : {Encoding.UTF8.GetString(resp.Payload[16..])} [{sw.ElapsedMilliseconds} ms]");
            }
            catch (TimeoutException e)
            {
                Console.WriteLine($"{targetHost} Offline {e.Message}");
            }
        }
        if (x.StartsWith("> message "))
        {
            x = x[10..].Trim();
            var spaceIndex = x.IndexOf(' ');
            if (spaceIndex < 0) throw new Exception("Format :\n> message target_host content...");
            var targetHost = x[..spaceIndex];
            var content = x[(spaceIndex + 1)..];
            // await endpoint.SendAsync(endpoint.BuildMessage(targetHost, Encoding.UTF8.GetBytes(content)));
            await fiber.SendMessageAsync(targetHost, Encoding.UTF8.GetBytes(content));
        }
    }
    catch (Exception e)
    {
        Console.Error.WriteLine(e);
    }

}