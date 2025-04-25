using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using VideoLibrary;
using TG_Weather_Bot.Video;
using System.Diagnostics;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace TG_Weather_Bot.Bot
{
    class BotUserInputHandler
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
        }

       

        public static async Task HandleVideoUrlInput(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Получен ввод от пользователя {message.Chat.Id}: {message.Text}");
            var chatId = message.Chat.Id;
            var videoUrl = message.Text;

            var videoId = VideoId.TryParse(videoUrl);
            Console.WriteLine($"Результат парсингу VideoId: {videoId}");

            if (videoId == null)
            {
                await botClient.SendMessage(chatId, "Недійсний URL відео.", cancellationToken: cancellationToken);
                BotCommands.ResetUserState(chatId);
                return;
            }

            try
            {
                var youtube = new YoutubeClient();
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId.Value);

                
                var audioStreams = streamManifest.GetAudioOnlyStreams().OrderByDescending(s => s.Bitrate);
                var bestAudioStream = audioStreams.FirstOrDefault();

                
                var videoStreams = streamManifest.GetVideoOnlyStreams().OrderByDescending(s => s.VideoResolution.Height);
                var bestVideoStream = videoStreams.FirstOrDefault();

                if (bestAudioStream != null && bestVideoStream != null)
                {
                    string tempDir = Path.Combine(Path.GetTempPath(), "YourBotName_VideoDownloads");
                    if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                    string videoFilePath = Path.Combine(tempDir, $"{videoId.Value}_video.mp4");
                    string audioFilePath = Path.Combine(tempDir, $"{videoId.Value}_audio.mp4");
                    string finalFilePath = Path.Combine(tempDir, $"{videoId.Value}_final.mp4");

                    await botClient.SendMessage(chatId, "Завантажуємо аудіо та відео...", cancellationToken: cancellationToken);
                    Console.WriteLine($"Завантажуємо відео в: {videoFilePath}");
                    Console.WriteLine($"Завантажуємо аудіо в: {audioFilePath}");

                    try
                    {
                        await youtube.Videos.Streams.DownloadAsync(bestVideoStream, videoFilePath);
                        Console.WriteLine($"Відео завантажено.");
                        await youtube.Videos.Streams.DownloadAsync(bestAudioStream, audioFilePath);
                        Console.WriteLine($"Аудіо завантажено.");

                        await botClient.SendMessage(chatId, "Об'єднуємо аудіо та відео...", cancellationToken: cancellationToken);
                        Console.WriteLine($"Об'єднуємо в: {finalFilePath}");

                        using (var process = new Process())
                        {
                            process.StartInfo = new ProcessStartInfo
                            {
                                FileName = "ffmpeg", 
                                Arguments = $"-i \"{videoFilePath}\" -i \"{audioFilePath}\" -acodec copy -vcodec copy \"{finalFilePath}\"",
                                CreateNoWindow = true,
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true
                            };

                            process.Start();
                            string output = await process.StandardOutput.ReadToEndAsync();
                            string errorOutput = await process.StandardError.ReadToEndAsync();
                            await process.WaitForExitAsync();

                            Console.WriteLine($"FFmpeg Output: {output}");
                            Console.WriteLine($"FFmpeg Error: {errorOutput}");
                            Console.WriteLine($"FFmpeg Exit Code: {process.ExitCode}");

                            if (process.ExitCode == 0)
                            {
                                await botClient.SendMessage(chatId, "Відео об'єднано. Відправляю...", cancellationToken: cancellationToken);
                                using (var videoStream = new FileStream(finalFilePath, FileMode.Open, FileAccess.Read))
                                {
                                    await botClient.SendVideo(
                                        chatId: chatId,
                                        video: new InputFileStream(videoStream, $"{videoId.Value}_final.mp4"),
                                        supportsStreaming: true,
                                        cancellationToken: cancellationToken);
                                    Console.WriteLine($"Відео успішно надіслано.");
                                }
                            }
                            else
                            {
                                await botClient.SendMessage(chatId, $"Помилка об'єднання відео (FFmpeg): {errorOutput}", cancellationToken: cancellationToken);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Помилка завантаження або об'єднання: {ex.Message}");
                        await botClient.SendMessage(chatId, $"Виникла помилка: {ex.Message}", cancellationToken: cancellationToken);
                    }
                    finally
                    {           
                        CleanupTempFiles(videoFilePath, audioFilePath, finalFilePath);
                        BotCommands.ResetUserState(chatId);
                    }
                }
                else
                {
                    await botClient.SendMessage(chatId, "Не знайдено окремих аудіо та відео потоків у достатній якості.", cancellationToken: cancellationToken);
                    BotCommands.ResetUserState(chatId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка обробки URL: {ex.Message}");
                await botClient.SendMessage(chatId, $"Виникла помилка: {ex.Message}", cancellationToken: cancellationToken);
                BotCommands.ResetUserState(chatId);
            }
        }

        private static void CleanupTempFiles(string videoPath, string audioPath, string finalPath)
        {
            try
            {
                if (File.Exists(videoPath)) File.Delete(videoPath);
                if (File.Exists(audioPath)) File.Delete(audioPath);
                if (File.Exists(finalPath)) File.Delete(finalPath);
                Console.WriteLine("Тимчасові файли видалено.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при видаленні тимчасових файлів: {ex.Message}");
            }
        }




        public static async Task HandleCityInput(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (BotCommands.IsAwaitingCity(message.Chat.Id))
            {
                string city = message.Text;
                BotCommands.LastCityInput[message.Chat.Id] = city;

                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("На сьогодні", "weather_today"),
                        InlineKeyboardButton.WithCallbackData("На 3 дні", "weather_3days"),
                    },
                });

                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Ви обрали місто: {city}. Для якого періоду ви хочете дізнатися прогноз погоди?",
                    replyMarkup: inlineKeyboard,
                    cancellationToken: cancellationToken);

                
                BotCommands.ResetUserState(message.Chat.Id);
            }
            else
            {
                
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Щоб дізнатися погоду, натисніть кнопку 'Погода' в меню /start.",
                    cancellationToken: cancellationToken);
            }
        }             
    }
}

