using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OurSpace.API.Data;
using OurSpace.API.Models.Entities;
using Xabe.FFmpeg;

namespace OurSpace.API.Services;

public class AudioConversionJobHandler(
    AppDbContext db,
    IFileStorageService fileStorage) : IBackgroundJobHandler
{
    public string JobType => BackgroundJobTypes.AudioConversion;

    public async Task HandleAsync(BackgroundJob job, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<AudioConversionPayload>(job.Payload)
            ?? throw new InvalidOperationException("Audio conversion payload could not be read.");

        var audio = await db.AudioMessages.SingleOrDefaultAsync(a => a.Id == payload.AudioId, cancellationToken);
        if (audio is null)
        {
            await fileStorage.DeleteAsync(payload.SourcePath);
            return;
        }

        try
        {
            var converted = await ConvertToMp3Async(payload.SourcePath, cancellationToken);

            audio.FilePath = converted.Path;
            audio.SizeBytes = converted.SizeBytes;
            audio.Status = AudioStatus.Ready;
            await db.SaveChangesAsync(cancellationToken);

            await fileStorage.DeleteAsync(payload.SourcePath);
        }
        catch (Exception)
        {
            audio.Status = AudioStatus.Failed;
            await db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task<StoredFile> ConvertToMp3Async(string videoUrl, CancellationToken cancellationToken)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var videoPath = Path.Combine(tempDir, "input.mp4");
            var mp3Path = Path.Combine(tempDir, "output.mp3");

            await using (var videoStream = await fileStorage.OpenReadAsync(videoUrl))
            await using (var videoFile = File.Create(videoPath))
            {
                await videoStream.CopyToAsync(videoFile, cancellationToken);
            }

            var conversion = FFmpeg.Conversions.New()
                .AddParameter($"-i \"{videoPath}\"")
                .AddParameter("-vn -acodec libmp3lame -q:a 2")
                .SetOutput(mp3Path);

            await conversion.Start(cancellationToken);

            await using var mp3Stream = File.OpenRead(mp3Path);
            return await fileStorage.SaveAsync(mp3Stream, "audio", ".mp3");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
