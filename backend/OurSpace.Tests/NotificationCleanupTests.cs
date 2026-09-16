using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OurSpace.API.Data;
using OurSpace.API.Models.Entities;
using OurSpace.API.Services;
using Xunit;

namespace OurSpace.Tests;

public class NotificationCleanupTests
{
    [Fact]
    public async Task Sending_A_Notification_Clears_Out_Long_Read_Ones()
    {
        using var factory = new OurSpaceFactory();
        var (recipientId, actorId) = await SeedPairAsync(factory);

        await AddNotificationAsync(factory, recipientId, actorId, "read-long-ago", readAt: DateTime.UtcNow.AddDays(-40));
        await AddNotificationAsync(factory, recipientId, actorId, "read-just-now", readAt: DateTime.UtcNow);

        await NotifyAsync(factory, recipientId, actorId, "brand-new");

        var remaining = await TitlesAsync(factory, recipientId);

        Assert.DoesNotContain("read-long-ago", remaining);
        Assert.Contains("read-just-now", remaining);
        Assert.Contains("brand-new", remaining);
    }

    [Fact]
    public async Task Unread_Notifications_Are_Never_Pruned()
    {
        using var factory = new OurSpaceFactory();
        var (recipientId, actorId) = await SeedPairAsync(factory);

        await AddNotificationAsync(factory, recipientId, actorId, "old-but-unread",
            readAt: null, createdAt: DateTime.UtcNow.AddDays(-400));

        await NotifyAsync(factory, recipientId, actorId, "brand-new");

        Assert.Contains("old-but-unread", await TitlesAsync(factory, recipientId));
    }

    [Fact]
    public async Task Cleanup_Never_Touches_Another_Users_Notifications()
    {
        using var factory = new OurSpaceFactory();
        var (recipientId, actorId) = await SeedPairAsync(factory);
        var otherId = await SeedUserAsync(factory, "lejla");

        await AddNotificationAsync(factory, otherId, actorId, "someone-elses-old-read",
            readAt: DateTime.UtcNow.AddDays(-40));

        await NotifyAsync(factory, recipientId, actorId, "brand-new");

        Assert.Contains("someone-elses-old-read", await TitlesAsync(factory, otherId));
    }

    private static async Task NotifyAsync(OurSpaceFactory factory, int recipientId, int actorId, string title)
    {
        using var scope = factory.Services.CreateScope();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        await notifications.NotifyAsync(recipientId, actorId, NotificationType.EventCreated, "event", 1, title);
    }

    private static async Task<List<string>> TitlesAsync(OurSpaceFactory factory, int recipientId)
    {
        using var scope = factory.Services.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Notifications
            .Where(n => n.RecipientUserId == recipientId)
            .Select(n => n.EntityTitle)
            .ToListAsync();
    }

    private static async Task AddNotificationAsync(
        OurSpaceFactory factory, int recipientId, int actorId, string title,
        DateTime? readAt, DateTime? createdAt = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Notifications.Add(new Notification
        {
            RecipientUserId = recipientId,
            ActorUserId = actorId,
            Type = NotificationType.EventCreated,
            EntityType = "event",
            EntityId = 1,
            EntityTitle = title,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            ReadAt = readAt,
        });

        await db.SaveChangesAsync();
    }

    private static async Task<(int RecipientId, int ActorId)> SeedPairAsync(OurSpaceFactory factory) =>
        (await SeedUserAsync(factory, "marko"), await SeedUserAsync(factory, "ana"));

    private static async Task<int> SeedUserAsync(OurSpaceFactory factory, string username)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Username = username,
            Email = $"{username}@test.local",
            PasswordHash = "not-a-real-hash",
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user.Id;
    }
}
