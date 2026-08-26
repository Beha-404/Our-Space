using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Data;
using OurSpace.API.Services;
using Xunit;

namespace OurSpace.Tests;

public class StorageQuotaTests
{
    [Fact]
    public async Task Usage_Sums_Photos_And_Audio_Of_The_Couple_Only()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var coupleAId = await SetSizesAsync(factory, world.PhotoId, 300, world.AudioId, 200);

        using var scope = factory.Services.CreateScope();
        var quota = scope.ServiceProvider.GetRequiredService<IStorageQuotaService>();

        Assert.Equal(500, await quota.GetUsedBytesAsync(coupleAId));
        Assert.Equal(0, await quota.GetUsedBytesAsync(coupleAId + 1));
    }

    [Fact]
    public async Task Upload_Is_Rejected_When_It_Would_Exceed_The_Quota()
    {
        using var factory = new OurSpaceFactory(quotaBytes: 1000);
        var world = await TestWorld.SeedAsync(factory);

        var coupleAId = await SetSizesAsync(factory, world.PhotoId, 900, world.AudioId, 0);

        using var scope = factory.Services.CreateScope();
        var quota = scope.ServiceProvider.GetRequiredService<IStorageQuotaService>();

        await Assert.ThrowsAsync<BadRequestException>(() => quota.EnsureRoomAsync(coupleAId, 200));
    }

    [Fact]
    public async Task Upload_Is_Allowed_When_It_Fits()
    {
        using var factory = new OurSpaceFactory(quotaBytes: 1000);
        var world = await TestWorld.SeedAsync(factory);

        var coupleAId = await SetSizesAsync(factory, world.PhotoId, 900, world.AudioId, 0);

        using var scope = factory.Services.CreateScope();
        var quota = scope.ServiceProvider.GetRequiredService<IStorageQuotaService>();

        await quota.EnsureRoomAsync(coupleAId, 100);
    }

    private static async Task<int> SetSizesAsync(OurSpaceFactory factory, int photoId, long photoBytes, int audioId, long audioBytes)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var photo = await db.Photos.SingleAsync(p => p.Id == photoId);
        photo.SizeBytes = photoBytes;

        var audio = await db.AudioMessages.SingleAsync(a => a.Id == audioId);
        audio.SizeBytes = audioBytes;

        await db.SaveChangesAsync();

        return photo.CoupleId;
    }
}
