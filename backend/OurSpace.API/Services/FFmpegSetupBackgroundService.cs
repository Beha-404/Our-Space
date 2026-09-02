using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace OurSpace.API.Services;

public class FFmpegSetupBackgroundService(
    IWebHostEnvironment env,
    IFFmpegReadiness readiness,
    ILogger<FFmpegSetupBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ffmpegPath = Path.Combine(env.ContentRootPath, "ffmpeg");

        try
        {
            Directory.CreateDirectory(ffmpegPath);
            FFmpeg.SetExecutablesPath(ffmpegPath);

            if (!BinariesPresent(ffmpegPath))
            {
                logger.LogInformation("FFmpeg binaries missing, downloading in the background");
                await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegPath);
            }

            if (BinariesPresent(ffmpegPath))
            {
                readiness.MarkReady();
                logger.LogInformation("FFmpeg is ready");
            }
            else
            {
                logger.LogWarning("FFmpeg binaries still missing after download attempt");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FFmpeg setup failed; video uploads will be rejected until it succeeds");
        }
    }

    private static bool BinariesPresent(string ffmpegPath)
    {
        var suffix = OperatingSystem.IsWindows() ? ".exe" : "";

        return File.Exists(Path.Combine(ffmpegPath, $"ffmpeg{suffix}"))
            && File.Exists(Path.Combine(ffmpegPath, $"ffprobe{suffix}"));
    }
}
