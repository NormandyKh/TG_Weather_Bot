using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TG_Weather_Bot.Weather
{
    class WeatherService
    {
        private static string ApiKey;
        private static readonly string ApiUrlCurrent = "https://api.openweathermap.org/data/2.5/weather?q={0}&appid={1}&units=metric&lang=uk";
        private static readonly string ApiUrlForecast = "https://api.openweathermap.org/data/2.5/forecast?q={0}&appid={1}&units=metric&lang=uk";
        private static readonly HttpClient client = new HttpClient();

        static WeatherService()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            ApiKey = configuration["OpenWeatherMap:ApiKey"];
            Console.WriteLine($"Считанный API ключ: [{ApiKey}]");
        }

        public static async Task<string> GetWeatherAsync(string city)
        {
            try
            {

                string encodedCity = Uri.EscapeDataString(city);
                string url = string.Format(ApiUrlCurrent, encodedCity, ApiKey);

                Console.WriteLine($"Requesting weather data from URL: {url}");

                HttpResponseMessage response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Response status code: {response.StatusCode}");
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return "Місто не знайдено. Перевірте назву міста.";
                    }
                    else
                    {
                        return $"Помилка: не вдалося отримати погоду. Код помилки: {response.StatusCode}";
                    }
                }

                string result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response content: {result}");
                JObject json = JObject.Parse(result);

                string description = json["weather"]?[0]?["description"]?.ToString() ?? "Немає даних.";
                double temp = json["main"]?["temp"]?.ToObject<double>() ?? 0;
                double feelsLike = json["main"]?["feels_like"]?.ToObject<double>() ?? 0;
                int humidity = json["main"]?["humidity"]?.ToObject<int>() ?? 0;

                return $" Погода в {city}:\n" +
                       $"Опис: {description}\n" +
                       $"Температура: **{temp}°C** (відчувається як **{feelsLike}°C**)\n" +
                       $"Вологість: **{humidity}%**";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при отриманні погоди: {ex.Message}");
                return "Помилка: не вдалося отримати погоду. Спробуйте пізніше.";
            }
        }

        public static async Task<string> GetWeatherForecast3DaysAsync(string city)
        {
            return await GetWeatherForecastAsync(city, 3);
        }

        private static async Task<string> GetWeatherForecastAsync(string city, int days)
        {
            try
            {
                string encodedCity = Uri.EscapeDataString(city);
                string url = string.Format(ApiUrlForecast, encodedCity, ApiKey);

                HttpResponseMessage response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return "Місто не знайдено. Перевірте назву міста.";
                    }
                    else
                    {
                        return $"Помилка: не вдалося отримати прогноз погоди. Код помилки: {response.StatusCode}";
                    }
                }

                string result = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(result);

                var forecastList = json["list"]?.ToObject<JArray>();
                if (forecastList == null || !forecastList.Any())
                {
                    return "Прогноз погоди недоступний для цього міста.";
                }

                var filteredForecasts = forecastList
                    .Where(item =>
                    {
                        DateTime forecastTime = item["dt_txt"]?.ToObject<DateTime>() ?? DateTime.MinValue;
                        return forecastTime > DateTime.Now && forecastTime < DateTime.Now.AddDays(days + 1);
                    })
                    .GroupBy(item => item["dt_txt"]?.ToObject<DateTime>().Date)
                    .Take(days)
                    .Select(group =>
                    {
                        var dayForecasts = group.ToList();
                        double avgTemp = dayForecasts.Average(f => f["main"]?["temp"]?.ToObject<double>() ?? 0);
                        string mainDescription = dayForecasts.FirstOrDefault()?["weather"]?[0]?["description"]?.ToString() ?? "Немає даних";
                        double feelsLike = dayForecasts.FirstOrDefault()?["main"]?["feels_like"]?.ToObject<double>() ?? 0;
                        return $"🗓️ {group.Key?.ToShortDateString()}:\n" +
                               $"{mainDescription}\n" +
                               $"Середня температура: {avgTemp:F1}°C\n"+
                               $"Відчувається як {feelsLike}°C";
                    })
                    .ToList();

                if (!filteredForecasts.Any())
                {
                    return $"Прогноз погоди на {days} дн. недоступний.";
                }

                return $"🌤 Прогноз погоди в {city} на {days} дн.:\n" + string.Join("\n", filteredForecasts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при отриманні прогнозу погоди: {ex.Message}");
                return "Помилка: не вдалося отримати прогноз погоди. Спробуйте пізніше.";
            }
        }
    } 
}
