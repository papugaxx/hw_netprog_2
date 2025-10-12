using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== Поиск информации о фильме ===\n");

        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        string apiKey = config["ApiKey"];

        Console.Write("Введите название фильма: ");
        string movieName = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(movieName))
        {
            Console.WriteLine("Название фильма не может быть пустым.");
            return;
        }

        using (HttpClient client = new HttpClient())
        {
            string url = $"https://api.themoviedb.org/3/search/movie?api_key={apiKey}&query={Uri.EscapeDataString(movieName)}&language=ru-RU";

            HttpResponseMessage response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ошибка при запросе: {response.StatusCode}");
                return;
            }

            string json = await response.Content.ReadAsStringAsync();

            MovieSearchResult result = JsonSerializer.Deserialize<MovieSearchResult>(json);

            if (result == null || result.results.Length == 0)
            {
                Console.WriteLine("Фильм не найден.");
                return;
            }

            Movie movie = result.results[0];

            Console.WriteLine("\n=== Информация о фильме ===");
            Console.WriteLine($"Название: {movie.title}");
            Console.WriteLine($"Оригинальное название: {movie.original_title}");
            Console.WriteLine($"Дата выхода: {movie.release_date}");
            Console.WriteLine($"Описание: {movie.overview}");
            Console.WriteLine($"Рейтинг: {movie.vote_average}/10");
        }
      

        Console.WriteLine("\nНажмите любую клавишу для выхода...");
        Console.ReadKey();
    }
}
public class MovieSearchResult
{
    public Movie[] results { get; set; }
}

public class Movie
{
    public string title { get; set; }
    public string original_title { get; set; }
    public string release_date { get; set; }
    public string overview { get; set; }
    public double vote_average { get; set; }
}
