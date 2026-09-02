using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OurSpace.API.Data;
using OurSpace.API.Models.Entities;
using OurSpace.API.Services;
using Xunit;

namespace OurSpace.Tests;

public class DeletedUserCleanupTests
{
    [Fact]
    public async Task Old_Soft_Deleted_Users_Are_Hard_Deleted_And_Free_Their_Username()
    {
        using var factory = new OurSpaceFactory();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new User
            {
                Username = "leftover",
                Email = "leftover@test.local",
                PasswordHash = "not-a-real-hash",
                IsDeleted = true,
            });
            await db.SaveChangesAsync();
        }

        await RunCleanupAsync(factory);

        using var check = factory.Services.CreateScope();
        var checkDb = check.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await checkDb.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == "leftover"));
    }

    [Fact]
    public async Task Cleanup_Also_Removes_Content_A_Pre_Fix_Deletion_Left_Behind()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ana = await db.Users.SingleAsync(u => u.Username == "ana");
            ana.IsDeleted = true;
            await db.SaveChangesAsync();
        }

        await RunCleanupAsync(factory);

        using var check = factory.Services.CreateScope();
        var checkDb = check.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await checkDb.Photos.IgnoreQueryFilters().AnyAsync(p => p.Id == world.PhotoId));
        Assert.False(await checkDb.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == "ana"));
        Assert.Equal(1, await checkDb.Couples.IgnoreQueryFilters().CountAsync());
        Assert.True(await checkDb.Users.AnyAsync(u => u.Username == "lejla"));
    }

    [Fact]
    public async Task Cleanup_Is_A_No_Op_When_Nothing_Is_Soft_Deleted()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        await RunCleanupAsync(factory);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(4, await db.Users.CountAsync());
    }

    private static async Task RunCleanupAsync(OurSpaceFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

        var service = new DeletedUserCleanupBackgroundService(
            scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<DeletedUserCleanupBackgroundService>.Instance);

        await service.RunOnceAsync(db, storage, CancellationToken.None);
    }
}
