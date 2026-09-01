using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Infrastructure.Services.Video
{
    public class FfmpegVideoThumbnailService
    : IVideoThumbnailService
    {
        private readonly string _ffmpegPath;
        private readonly ILogger<FfmpegVideoThumbnailService> _logger;

        public FfmpegVideoThumbnailService(
            IConfiguration configuration,
            ILogger<FfmpegVideoThumbnailService> logger)
        {
            _logger = logger;

            _ffmpegPath =
                configuration["FFmpeg:Path"]
                ?? "ffmpeg";
        }

        public async Task GenerateThumbnailAsync(
            string videoPath,
            string thumbnailPath,
            CancellationToken cancellationToken = default)
        {
            var directory =
                Path.GetDirectoryName(thumbnailPath);

            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var arguments =
                $"-ss 00:00:01 " +
                $"-i \"{videoPath}\" " +
                $"-frames:v 1 " +
                $"-q:v 2 " +
                $"\"{thumbnailPath}\" " +
                $"-y";

            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process =
                new Process
                {
                    StartInfo = startInfo
                };

            process.Start();

            var errorTask =
                process.StandardError.ReadToEndAsync();

            var outputTask =
                process.StandardOutput.ReadToEndAsync();

            await process.WaitForExitAsync(
                cancellationToken);

            var error =
                await errorTask;

            await outputTask;

            if (process.ExitCode != 0)
            {
                _logger.LogError(
                    "FFmpeg thumbnail generation failed. " +
                    "Video={Video}, ExitCode={ExitCode}, Error={Error}",
                    videoPath,
                    process.ExitCode,
                    error);

                throw new InvalidOperationException(
                    $"FFmpeg failed to generate thumbnail. " +
                    $"ExitCode={process.ExitCode}");
            }

            if (!File.Exists(thumbnailPath))
            {
                throw new InvalidOperationException(
                    "FFmpeg completed but thumbnail file was not created.");
            }
        }
    }
}
