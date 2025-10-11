// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Text;
using FiberDistro.Naive;

// var server = new Server(9877, async (payload, s) =>
// {
//     Console.WriteLine(Encoding.UTF8.GetString(payload));
//     return "ack"u8.ToArray();
// });
// Console.WriteLine();
// while (true)
// {
//     var x = Console.ReadLine();
//     if (x == null) continue;
//     if (x.StartsWith("exit"))
//     {
//         break;
//     }
// }

using NavieClient = FiberDistro.Naive.Client;
using NavieServer = FiberDistro.Naive.Server;

var counter = 0;
var server = new NavieServer(9876, (data, endpoint) =>
{
    counter++;
    var s = Encoding.UTF8.GetString(data);
    Console.WriteLine($"- Client({endpoint}) said : {s}");
    return Task.FromResult(Encoding.UTF8.GetBytes($"ACK {counter} {s}"));
}); 
Thread.Sleep(1000);

var cli = new NavieClient(IPEndPoint.Parse($"10.1.16.37:9876"));

cli.Received += data =>
{
    Console.WriteLine("- Server said : " + Encoding.UTF8.GetString(data));
};

while (true)
{
    var x = Console.ReadLine();
    if (x == null) break;
    x = x.Trim();
    try
    {
        if (x.Equals("exit")) break;
        await cli.SendAsync(Encoding.UTF8.GetBytes(x));
    }
    catch (Exception ex)
    {
        
    }
}