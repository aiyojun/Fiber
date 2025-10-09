// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;
using System.Text;
using Fiber.Core;

var addr = "192.168.1.10:51243";
var endpoint = IPEndPoint.Parse(addr);
Console.WriteLine(endpoint.ToString());
Console.WriteLine(endpoint.Port);
// Console.WriteLine(endpoint.Address.ToString());
// var buf = endpoint.Serialize().Buffer.ToArray();
// var buf2 = Helper.ParseAddress(addr);
// Console.WriteLine(BitConverter.ToString(buf) + " : " + buf.Length);
// Console.WriteLine(BitConverter.ToString(buf2) + " : " + buf2.Length);
//
// var socketAddress = new SocketAddress(buf.Length == 16 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6, buf.Length);
// for (var i = 0; i < buf.Length; i++) { socketAddress[i] = buf[i]; }
// Console.WriteLine(new IPEndPoint(0, 0).Create(socketAddress).ToString());



// using System.Text;
//
// var server = new Fiber.Naive.Server(9877, async (payload, s) =>
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