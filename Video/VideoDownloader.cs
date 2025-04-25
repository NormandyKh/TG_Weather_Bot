using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using System.Diagnostics;
using VideoLibrary;


namespace TG_Weather_Bot.Video
{
    class VideoDownloader
    {

        public static async Task DownloadFileAsync(YouTubeVideo video, string filePath)
        {
            int totalBytes = 0;
            using (var fs = new FileStream(filePath, FileMode.Create))
            using (var stream = video.Stream())
            {
                byte[] buffer = new byte[128 * 1024];
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fs.WriteAsync(buffer, 0, bytesRead);
                    totalBytes += bytesRead;
                }
            }
        }

        public static async Task MergeFilesAsync(string videoPath, string audioPath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{videoPath}\" -i \"{audioPath}\" -acodec copy -vcodec copy \"{Directory.GetCurrentDirectory()}\\final.mp4\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();         
            string output = await process.StandardOutput.ReadToEndAsync();
            string errorOutput = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            Console.WriteLine($"FFmpeg Output: {output}");
            Console.WriteLine($"FFmpeg Error: {errorOutput}");

            if (process.ExitCode != 0)
            {
                Console.WriteLine($"FFmpeg exited with code: {process.ExitCode}");               
            }
        }

    }

}

