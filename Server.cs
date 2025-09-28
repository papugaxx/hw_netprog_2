using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    static readonly Dictionary<(string, string), double> Rates = new()
    {
        {("USD","EUR"), 0.92}, {("EUR","USD"), 1.08},
        {("USD","UAH"), 41.0}, {("UAH","USD"), 0.024}
    };

    static readonly int MaxRequestsPerSession = 3;
    static readonly int MaxClients = 2; 

    static readonly Dictionary<string, DateTime> BannedClients = new();
    static int CurrentClients = 0;

    static void Main()
    {
        var listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine(
            $"Сервер запущен... Лимит запросов: {MaxRequestsPerSession}, Макс клиентов: {MaxClients}");

        while (true)
        {
            var client = listener.AcceptTcpClient();

            if (Interlocked.CompareExchange(ref CurrentClients, 0, 0) >= MaxClients)
            {
                using var stream = client.GetStream();
                using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                writer.WriteLine("Сервер перегружен. Попробуйте подключиться позже.");
                Log("Отклонено подключение: сервер достиг лимита клиентов.");
                client.Close();
                continue;
            }

            Interlocked.Increment(ref CurrentClients);
            _ = Task.Run(() => HandleClient(client));
        }
    }

    static void HandleClient(TcpClient client)
    {
        string clientInfo = client.Client.RemoteEndPoint?.ToString() ?? "неизвестно";
        Log($"Подключение: {clientInfo}");

        try
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            writer.WriteLine("Введите: USD EUR или QUIT");

            int requestCount = 0;

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (BannedClients.TryGetValue(clientInfo, out var until))
                {
                    if (DateTime.Now < until)
                    {
                        var remaining = (int)(until - DateTime.Now).TotalSeconds;
                        writer.WriteLine($"Вы заблокированы. Подождите {remaining} секунд.");
                        continue; 
                    }
                    else
                    {
                        BannedClients.Remove(clientInfo);
                        requestCount = 0;
                    }
                }

                if (line.ToUpper() == "QUIT") break;

                requestCount++;
                if (requestCount > MaxRequestsPerSession)
                {
                    var banUntil = DateTime.Now.AddMinutes(1);
                    BannedClients[clientInfo] = banUntil;
                    writer.WriteLine("Вы превысили лимит запросов. Заблокированы на 60 секунд.");
                    Log($"Блокировка клиента {clientInfo} до {banUntil}");
                    continue;
                }

                var parts = line.Split(' ');
                if (parts.Length == 2 &&
                    Rates.TryGetValue((parts[0].ToUpper(), parts[1].ToUpper()), out var rate))
                {
                    string response = $"{parts[0].ToUpper()} -> {parts[1].ToUpper()} = {rate}";
                    writer.WriteLine(response);
                    Log($"Запрос {clientInfo}: {response}");
                }
                else
                {
                    writer.WriteLine("Нет данных для этой пары.");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Ошибка клиента {clientInfo}: {ex.Message}");
        }
        finally
        {
            client.Close();
            Interlocked.Decrement(ref CurrentClients);
            Log($"Отключение: {clientInfo}. Текущих клиентов: {CurrentClients}");
        }
    }

    static void Log(string msg)
    {
        string line = $"[{DateTime.Now}] {msg}";
        Console.WriteLine(line);
    }
}
