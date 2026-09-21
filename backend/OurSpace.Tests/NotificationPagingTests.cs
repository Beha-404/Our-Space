using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Notification;
using OurSpace.API.Models.Entities;
using Xunit;

namespace OurSpace.Tests;

public class NotificationPagingTests
{
    private const int Total = 45;

    [Fact]
    public async Task Without_Parameters_The_Newest_Twenty_Come_Back()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);
        await AddNotificationsAsync(factory, "marko", Total);

        var client = TestWorld.ClientFor(factory, world.MarkoToken);
        var page = await client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications");

        Assert.NotNull(page);
        Assert.Equal(20, page.Count);
        Assert.Equal("Stavka 45", page[0].EntityTitle);
    }

    [Fact]
    public async Task Skip_Moves_On_To_The_Next_Slice_Without_Overlap()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);
        await AddNotificationsAsync(factory, "marko", Total);
        var client = TestWorld.ClientFor(factory, world.MarkoToken);

        var first = await client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications?skip=0&take=20");
        var second = await client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications?skip=20&take=20");
        var third = await client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications?skip=40&take=20");

        Assert.Equal(20, first!.Count);
        Assert.Equal(20, second!.Count);
        Assert.Equal(6, third!.Count);
        Assert.Empty(first.Select(n => n.Id).Intersect(second.Select(n => n.Id)));
        Assert.Empty(second.Select(n => n.Id).Intersect(third.Select(n => n.Id)));
    }

    [Fact]
    public async Task Skipping_Past_The_End_Returns_Nothing()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);
        var client = TestWorld.ClientFor(factory, world.MarkoToken);

        var page = await client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications?skip=500&take=20");

        Assert.NotNull(page);
        Assert.Empty(page);
    }

    [Fact]
    public async Task Page_Size_Is_Capped_And_Nonsense_Values_Are_Tamed()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);
        await AddNotificationsAsync(factory, "marko", 70);
        var client = TestWorld.ClientFor(factory, world.MarkoToken);

        var huge = await client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications?take=100000");
        var negative = await client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications?skip=-5&take=-3");

        Assert.Equal(50, huge!.Count);
        Assert.Single(negative!);
    }

    [Fact]
    public async Task Another_User_Never_Gets_Notifications_From_Someone_Elses_Pages()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);
        await AddNotificationsAsync(factory, "marko", Total);

        var client = TestWorld.ClientFor(factory, world.LejlaToken);
        var page = await client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications?skip=0&take=50");

        Assert.NotNull(page);
        Assert.Empty(page);
    }

    private static async Task AddNotificationsAsync(OurSpaceFactory factory, string recipientUsername, int count)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var recipientId = db.Users.Single(u => u.Username == recipientUsername).Id;
        var actorId = db.Users.Single(u => u.Username == "ana").Id;
        var start = DateTime.UtcNow;

        for (var i = 1; i <= count; i++)
        {
            db.Notifications.Add(new Notification
            {
                RecipientUserId = recipientId,
                ActorUserId = actorId,
                Type = NotificationType.PhotoAdded,
                EntityType = "photo",
                EntityId = i,
                EntityTitle = $"Stavka {i}",
                CreatedAt = start.AddSeconds(i),
            });
        }

        await db.SaveChangesAsync();
    }
}
