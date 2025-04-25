using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TG_Weather_Bot.Bot;
using TG_Weather_Bot.Weather;

class Program
{
    private static string token;
    private static TelegramBotClient bot;
    private static IConfiguration Configuration { get; set; }


    static async Task Main()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        Configuration = builder.Build();
        token = Configuration["TelegramBot:Token"];
        bot = new TelegramBotClient(token);

        using var cts = new CancellationTokenSource();

        var recieverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, recieverOptions, cts.Token);

        var me = await bot.GetMe();
        Console.WriteLine($"Start listening for @{me.Username}");

        await Task.Delay(-1);
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            { Message: { Text: "/start" } } => BotCommands.HandleStartCommand(botClient, update.Message, cancellationToken),
            { CallbackQuery: { Data: "get_weather" } } => BotCommands.HandleWeatherRequest(botClient, update.CallbackQuery.Message, cancellationToken),
            { Message: { Text: { } msg } } when BotCommands.IsAwaitingDownload(update.Message.Chat.Id) => BotUserInputHandler.HandleVideoUrlInput(botClient, update.Message, cancellationToken),            
            { Message: { Text: { } msg } } when BotCommands.IsAwaitingCity(update.Message.Chat.Id) => BotUserInputHandler.HandleCityInput(botClient, update.Message, cancellationToken),
            { CallbackQuery: { Data: "download_video" } } => BotCommands.HandleDownloadVideoRequest(botClient, update.CallbackQuery.Message, cancellationToken),
            { CallbackQuery: { Data: "weather_today" } } => BotCallbackQuery.HandleWeatherTodayCallback(botClient, update.CallbackQuery, cancellationToken),           
            { CallbackQuery: { Data: "weather_3days" } } => BotCallbackQuery.HandleWeather3DaysCallback(botClient, update.CallbackQuery, cancellationToken),
            { CallbackQuery: { } callbackQuery } => BotCallbackQuery.HandleCallbackQuery(botClient, callbackQuery, cancellationToken),
            { Message: { } message } => BotCommands.HandleUnknownCommand(botClient, message, cancellationToken),
            _ => Task.CompletedTask,
        };

        await handler;
    }

    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(exception.Message);
        return Task.CompletedTask;
    }

}
