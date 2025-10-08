// See https://aka.ms/new-console-template for more information

using System.Text;

var server = new Fiber.Naive.Server(9877, async (payload, s) =>
{
    Console.WriteLine(Encoding.UTF8.GetString(payload));
    return "ack"u8.ToArray();
});
Console.WriteLine();
while (true)
{
    var x = Console.ReadLine();
    if (x == null) continue;
    if (x.StartsWith("exit"))
    {
        break;
    }
}