using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class ClientUDP
{
    static void Main()
    {
        Console.Title = "Комплектующие";
        UdpClient client = new UdpClient();
        client.Connect("127.0.0.1", 8888);

        Console.WriteLine("Клиент подключён к серверу.");
        Console.WriteLine("Введите название комплектующего (processor, videocard, ram, ssd, motherboard)");
        Console.WriteLine("Для выхода напишите: exit");

        while (true)
        {
            Console.Write("\nВаш запрос: ");
            string message = Console.ReadLine();

            if (message.ToLower() == "exit") break;

            byte[] data = Encoding.UTF8.GetBytes(message);
            client.Send(data, data.Length);

            IPEndPoint remoteEP = null;
            byte[] received = client.Receive(ref remoteEP);
            string response = Encoding.UTF8.GetString(received);
            Console.WriteLine($"Ответ от сервера: {response}");
        }

        client.Close();
        Console.WriteLine("Клиент отключён.");
    }
}

