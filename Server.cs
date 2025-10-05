using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

public class ClientInfo
{
    public DateTime ConnectionTime { get; set; } = DateTime.Now;
    public DateTime LastActivityTime { get; set; } = DateTime.Now;
    public List<DateTime> RequestTimes { get; set; } = new List<DateTime>();
}

class ServerUDP
{
    static UdpClient server;
    static Dictionary<IPEndPoint, ClientInfo> clients = new Dictionary<IPEndPoint, ClientInfo>();
    static Dictionary<string, decimal> prices = new Dictionary<string, decimal>();
    static object locker = new object();
    static string logFile = "server_log.txt";

    static void Main()
    {
        Console.Title = "Комплектующие";
        server = new UdpClient(8888);

        prices["processor"] = 250;
        prices["videocard"] = 499;
        prices["ram"] = 120;
        prices["ssd"] = 89;
        prices["motherboard"] = 180;

        Thread cleaner = new Thread(RemoveInactiveClients);
        cleaner.Start();

        Console.WriteLine("Сервер запущен на порту 8888 ");

        while (true)
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = server.Receive(ref remoteEP);
            string request = Encoding.UTF8.GetString(data);

            lock (locker)
            {
                if (!clients.ContainsKey(remoteEP))
                {
                    clients[remoteEP] = new ClientInfo();
                    Log($"Новый клиент {remoteEP}");
                }

                clients[remoteEP].LastActivityTime = DateTime.Now;

                clients[remoteEP].RequestTimes.RemoveAll(t => (DateTime.Now - t).TotalHours > 1);
                if (clients[remoteEP].RequestTimes.Count >= 10)
                {
                    Send("Лимит 10 запросов в час превышен!", remoteEP);
                    Log($"Клиент {remoteEP} превысил лимит запросов");
                    continue;
                }

                clients[remoteEP].RequestTimes.Add(DateTime.Now);
            }

            string response = HandleRequest(request);
            Send(response, remoteEP);
            Log($"Запрос от {remoteEP}: \"{request}\" → Ответ: \"{response}\"");
        }
    }

    static string HandleRequest(string request)
    {
        string key = request.Trim().ToLower();
        if (prices.ContainsKey(key))
            return $"Цена на {key}: {prices[key]} $";
        else
            return $"Товар \"{request}\" не найден.";
    }

    static void Send(string message, IPEndPoint client)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        server.Send(data, data.Length, client);
    }

    static void RemoveInactiveClients()
    {
        while (true)
        {
            Thread.Sleep(60000); 
            lock (locker)
            {
                var toRemove = new List<IPEndPoint>();
                foreach (var c in clients)
                {
                    if ((DateTime.Now - c.Value.LastActivityTime).TotalMinutes > 10)
                        toRemove.Add(c.Key);
                }
                foreach (var r in toRemove)
                {
                    clients.Remove(r);
                    Log($"Клиент {r} отключен за неактивность");
                }
            }
        }
    }

    static void Log(string text)
    {
        string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {text}";
        Console.WriteLine(line);
        File.AppendAllText(logFile, line + Environment.NewLine);
    }
}
