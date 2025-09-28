using System.Net.Sockets;
using System.Text;

class Program
{
    static void Main()
    {
        using var client = new TcpClient("127.0.0.1", 5000);
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        Console.WriteLine(reader.ReadLine()); 

        while (true)
        {
            Console.Write("> ");
            var msg = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(msg)) continue;

            writer.WriteLine(msg);
            if (msg.ToUpper() == "QUIT") break;

            Console.WriteLine("Сервер: " + reader.ReadLine());
        }
    }
}
