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
            throw new BadRequestException(localizer.T("Storage.QuotaExceeded", Megabytes(quota), Megabytes(Math.Max(quota - used, 0))));
    }

    private static long Megabytes(long bytes) => bytes / (1024 * 1024);
}
