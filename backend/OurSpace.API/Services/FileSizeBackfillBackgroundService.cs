using Microsoft.EntityFrameworkCore;
using OurSpace.API.Data;

namespace OurSpace.API.Services;

public class FileSizeBackfillBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<FileSizeBackfillBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

            var photos = await db.Photos.Where(p => p.SizeBytes == 0).ToListAsync(stoppingToken);
            foreach (var photo in photos)
                photo.SizeBytes = storage.GetSizeBytes(photo.FilePath) + storage.GetSizeBytes(photo.ThumbnailPath);

            var audio = await db.AudioMessages.Where(a => a.SizeBytes == 0).ToListAsync(stoppingToken);
            foreach (var message in audio)
                message.SizeBytes = storage.GetSizeBytes(message.FilePath);

            if (photos.Count == 0 && audio.Count == 0)
                return;

            await db.SaveChangesAsync(stoppingToken);
            logger.LogInformation("Backfilled sizes for {Photos} photos and {Audio} audio messages", photos.Count, audio.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "File size backfill failed");
        }
    }
}
