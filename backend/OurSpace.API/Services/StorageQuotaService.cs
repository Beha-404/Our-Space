using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;
using OurSpace.API.Options;

namespace OurSpace.API.Services;

public class StorageQuotaService(
    AppDbContext db,
    IOptions<StorageOptions> storageOptions,
    IEmailQueue emailQueue,
    ILocalizer localizer) : IStorageQuotaService
{
    public async Task<long> GetUsedBytesAsync(int coupleId)
    {
        var photoBytes = await db.Photos
            .Where(p => p.CoupleId == coupleId)
            .SumAsync(p => (long?)p.SizeBytes) ?? 0;

        var audioBytes = await db.AudioMessages
            .Where(a => a.CoupleId == coupleId)
            .SumAsync(a => (long?)a.SizeBytes) ?? 0;

        return photoBytes + audioBytes;
    }

    public async Task EnsureRoomAsync(int coupleId, long incomingBytes)
    {
        var quota = storageOptions.Value.QuotaBytesPerCouple;

        if (quota <= 0)
            return;

        var used = await GetUsedBytesAsync(coupleId);

        if (used + incomingBytes > quota)
        {
            await NotifyQuotaReachedOnceAsync(coupleId, quota);
            throw new BadRequestException(localizer.T("Storage.QuotaExceeded", Megabytes(quota), Megabytes(Math.Max(quota - used, 0))));
        }
    }

    private async Task NotifyQuotaReachedOnceAsync(int coupleId, long quota)
    {
        var couple = await db.Couples.Include(c => c.User1).Include(c => c.User2)
            .SingleOrDefaultAsync(c => c.Id == coupleId);

        if (couple is null || couple.QuotaWarningEmailSentAt is not null)
            return;

        var gigabytes = quota / (1024 * 1024 * 1024);

        foreach (var user in new[] { couple.User1, couple.User2 })
        {
            var subject = localizer.For("Email.StorageQuotaReached.Subject", user.PreferredLanguage);
            var body = localizer.For("Email.StorageQuotaReached.Body", user.PreferredLanguage, gigabytes);
            await emailQueue.EnqueueAsync(user.Email, subject, body);
        }

        couple.QuotaWarningEmailSentAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private static long Megabytes(long bytes) => bytes / (1024 * 1024);
}
