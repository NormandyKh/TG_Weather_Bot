using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace TG_Weather_Bot.Bot
{
    class BotCommands
    {
        private enum BotState
        {
            WaitingForCity,
            WaitingForVideoUrl,
            WaitingForCurrency,
            None
        }

        private static readonly Dictionary<long, BotState> UserState = new Dictionary<long, BotState>();
        public static readonly Dictionary<long, string> LastCityInput = new Dictionary<long, string>();


        public static async Task HandleStartCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Погода 🌤️", "get_weather"),
                    InlineKeyboardButton.WithCallbackData("Завантажити відео з Ютуб", "download_video"),
                    InlineKeyboardButton.WithCallbackData("Курс валют", "show_currency_keyboard")
                },
            });

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Привіт! Виберіть одну з опцій:",
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken);

            
            UserState.Remove(message.Chat.Id);
        }

        public static async Task SendCurrencyKeyboardAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
            new[] { InlineKeyboardButton.WithCallbackData("USD", "USD"), InlineKeyboardButton.WithCallbackData("EUR", "EUR") },
            new[] { InlineKeyboardButton.WithCallbackData("PLN", "PLN"), InlineKeyboardButton.WithCallbackData("GBP", "GBP") }
            });

            await botClient.SendMessage(
                chatId: chatId,
                text: "Виберіть валюту:",
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken);

            UserState[chatId] = BotState.WaitingForCurrency;
            Console.WriteLine("UserState set to WaitingForCurrency after sending keyboard");
        }



        public static async Task HandleDownloadVideoRequest(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Будь ласка, надішліть URL відео YouTube для завантаження:",
                cancellationToken: cancellationToken);

            UserState[message.Chat.Id] = BotState.WaitingForVideoUrl;
            Console.WriteLine("UserState set to WaitingForVideoUrl");
        }

        public static async Task HandleWeatherRequest(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Будь ласка, введіть назву міста:",
                replyMarkup: new ForceReplyMarkup { Selective = true },
                cancellationToken: cancellationToken);

            UserState[message.Chat.Id] = BotState.WaitingForCity;

        }

        public static async Task HandleUnknownCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Я поки не знаю цю команду.",
                cancellationToken: cancellationToken);
        }

        public static string GetLastCityInput(long chatId)
        {
            return LastCityInput.ContainsKey(chatId) ? LastCityInput[chatId] : null;
        }

        public static bool IsAwaitingDownload(long chatId)
        {
            return UserState.ContainsKey(chatId) && UserState[chatId] == BotState.WaitingForVideoUrl;
        }

        

        public static bool IsAwaitingCity(long chatId)
        {
            return UserState.ContainsKey(chatId) && UserState[chatId] == BotState.WaitingForCity;
        }

        public static void ResetUserState(long chatId)
        {
            UserState.Remove(chatId);
        }
    }
}
