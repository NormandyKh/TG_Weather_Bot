using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using TG_Weather_Bot.Weather;
using Telegram.Bot.Types.Enums;
using System.Text.Json;
using TG_Weather_Bot.Bot;

namespace TG_Weather_Bot.Bot
{
    class BotCallbackQuery
    {
        public static async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            if (callbackQuery.Data == "weather")
            {
                await botClient.SendMessage(chatId: callbackQuery.Message.Chat.Id, text: "Введіть місто для прогнозу погоди.");
            }
            else if (callbackQuery.Data == "download_video")
            {
                await botClient.SendMessage(chatId: callbackQuery.Message.Chat.Id, text: "Будь ласка, введіть URL відео YouTube для завантаження.");
            }
            else if (callbackQuery.Data == "show_currency_keyboard")
            {
                await BotCommands.SendCurrencyKeyboardAsync(botClient, callbackQuery.Message.Chat.Id, cancellationToken);
            }
            else if (callbackQuery.Data == "USD" || callbackQuery.Data == "EUR" || callbackQuery.Data == "PLN" || callbackQuery.Data == "GBP")
            {
                await GetAndSendCurrencyRateAsync(botClient, callbackQuery.Message.Chat.Id, callbackQuery.Data, callbackQuery.Message.MessageId, cancellationToken);
            }
        }
        
        public static async Task GetAndSendCurrencyRateAsync(ITelegramBotClient botClient, long chatId, string currencyCode, int messageId, CancellationToken cancellationToken)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync("https://bank.gov.ua/NBUStatService/v1/statdirectory/exchange?json", cancellationToken);
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var currencies = JsonSerializer.Deserialize<List<CurrencyInfo>>(content);

                    var selectedCurrency = currencies?.FirstOrDefault(c => c.cc == currencyCode);

                    if (selectedCurrency != null)
                    {
                        string message = $"Курс {selectedCurrency.txt} ({selectedCurrency.cc}) на {selectedCurrency.exchangedate}: {selectedCurrency.rate} UAH";
                        await botClient.EditMessageText(chatId: chatId, messageId: messageId, text: message, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await botClient.SendMessage(chatId: chatId, text: $"Не вдалося знайти курс для валюти {currencyCode}.", cancellationToken: cancellationToken);
                    }
                }
                catch (HttpRequestException ex)
                {
                    await botClient.SendMessage(chatId: chatId, text: $"Виникла помилка при отриманні курсу валют: {ex.Message}", cancellationToken: cancellationToken);
                }
                catch (JsonException ex)
                {
                    await botClient.SendMessage(chatId: chatId, text: $"Помилка обробки даних про курс валют: {ex.Message}", cancellationToken: cancellationToken);
                }
            }
        }

        public static async Task HandleWeatherTodayCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id);
            long chatId = callbackQuery.Message.Chat.Id;
            string city = BotCommands.GetLastCityInput(chatId);

            if (!string.IsNullOrEmpty(city))
            {
                string weatherInfo = await WeatherService.GetWeatherAsync(city);
                await botClient.SendMessage(chatId, weatherInfo, ParseMode.Markdown, cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendMessage(chatId, "Не вдалося визначити місто. Будь ласка, спробуйте ще раз, ввівши назву міста.", cancellationToken: cancellationToken);
            }
        }

        public static async Task HandleWeather3DaysCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id);
            long chatId = callbackQuery.Message.Chat.Id;
            string city = BotCommands.GetLastCityInput(chatId);

            if (!string.IsNullOrEmpty(city))
            {
                string forecastInfo = await WeatherService.GetWeatherForecast3DaysAsync(city);
                await botClient.SendMessage(chatId, forecastInfo, cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendMessage(chatId, "Не вдалося визначити місто. Будь ласка, спробуйте ще раз, ввівши назву міста.", cancellationToken: cancellationToken);
            }
        }
    }
}
