using Microsoft.EntityFrameworkCore;
using OurSpace.API.Data;

namespace OurSpace.API.Services;

public class DeletedUserCleanupBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<DeletedUserCleanupBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            await RunOnceAsync(
                scope.ServiceProvider.GetRequiredService<AppDbContext>(),
                scope.ServiceProvider.GetRequiredService<IFileStorageService>(),
                stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Deleted-user cleanup failed");
        }
    }

    public async Task RunOnceAsync(AppDbContext db, IFileStorageService storage, CancellationToken cancellationToken)
    {
        var staleUsers = await db.Users.IgnoreQueryFilters()
            .Where(u => u.IsDeleted)
            .ToListAsync(cancellationToken);

        if (staleUsers.Count == 0) return;

        foreach (var user in staleUsers)
        {
            var couple = await db.Couples.IgnoreQueryFilters()
                .SingleOrDefaultAsync(c => c.User1Id == user.Id || c.User2Id == user.Id, cancellationToken);

            if (couple is not null)
            {
                var photos = await db.Photos.IgnoreQueryFilters()
                    .Where(p => p.CoupleId == couple.Id)
                    .Select(p => new { p.FilePath, p.ThumbnailPath })
                    .ToListAsync(cancellationToken);

                var audioPaths = await db.AudioMessages.IgnoreQueryFilters()
                    .Where(a => a.CoupleId == couple.Id)
                    .Select(a => a.FilePath)
                    .ToListAsync(cancellationToken);

                foreach (var photo in photos)
                {
                    storage.Delete(photo.FilePath);
                    storage.Delete(photo.ThumbnailPath);
                }

                foreach (var path in audioPaths)
                    storage.Delete(path);

                db.Couples.Remove(couple);
            }

            if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
                storage.Delete(user.ProfilePictureUrl);

            db.Users.Remove(user);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Cleaned up {Count} previously soft-deleted account(s)", staleUsers.Count);
    }
}
