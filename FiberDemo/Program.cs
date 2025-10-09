// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Net;
using System.Text;
using Fiber.Core;

if (args.Length == 0) throw new Exception("Usage : fiber master_ip");
var masterIp = args[0];
var runAsClient = args.Length > 1 && args[1] == "cli";
var fiber = new Fiber.Core.Fiber(masterIp, runAsClient);
var endpoint = fiber.Endpoint;
endpoint.Received += packet => Console.WriteLine("[MESSAGE] " + Encoding.UTF8.GetString(packet.Payload)); 
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
        if (x.Equals("> list"))
        {
            var hosts = endpoint is Server server ? server.List() : await ((Client) endpoint).List();
            Console.WriteLine("Online endpoints :\n---");
            foreach (var host in hosts)
            {
                Console.WriteLine($"  {host}");
            }
            Console.WriteLine("---");
        }
        if (x.StartsWith("> ping "))
        {
            var targetHost = x[7..].Trim();
            var packet = endpoint.BuildRequest(targetHost, "ping"u8.ToArray());
            try
            {
                var sw = Stopwatch.StartNew();
                var resp = await endpoint.Request(packet);
                sw.Stop();
                Console.WriteLine($"{targetHost} Reply : {Encoding.UTF8.GetString(resp.Payload[16..])} [{sw.ElapsedMilliseconds} ms]");
            }
            catch (TimeoutException)
            {
                Console.WriteLine($"{targetHost} Offline");
            }
        }
        if (x.StartsWith("> message "))
        {
            x = x[10..].Trim();
            var spaceIndex = x.IndexOf(' ');
            if (spaceIndex < 0) throw new Exception("Format :\n> message target_host content...");
            var targetHost = x[..spaceIndex];
            var content = x[(spaceIndex + 1)..];
            await endpoint.SendAsync(endpoint.BuildMessage(targetHost, Encoding.UTF8.GetBytes(content)));
        }
    }
    catch (Exception e)
    {
        Console.Error.WriteLine(e);
    }

}