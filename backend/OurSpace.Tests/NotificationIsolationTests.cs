using System.Net.Http.Json;
using OurSpace.API.Models.DTOs.Event;
using OurSpace.API.Models.DTOs.Notification;
using Xunit;

namespace OurSpace.Tests;

public class NotificationIsolationTests
{
    [Fact]
    public async Task Recipient_Sees_Their_Notification()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.MarkoToken);
        var notifications = await client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications");

        Assert.NotNull(notifications);
        Assert.Contains(notifications, n => n.EntityTitle == TestWorld.EventTitle && n.ActorUsername == "ana");
    }

    [Fact]
    public async Task Actor_Does_Not_See_The_Notification_They_Caused()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.AnaToken);
        var notifications = await client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications");

        Assert.NotNull(notifications);
        Assert.Empty(notifications);
    }

    [Fact]
    public async Task Other_Couple_Does_Not_See_Notification()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.LejlaToken);
        var notifications = await client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications");

        Assert.NotNull(notifications);
        Assert.Empty(notifications);
    }

    [Fact]
    public async Task Unread_Count_Is_Scoped_To_The_Recipient()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var recipient = TestWorld.ClientFor(factory, world.MarkoToken);
        var stranger = TestWorld.ClientFor(factory, world.LejlaToken);

        Assert.Equal(1, await recipient.GetFromJsonAsync<int>("/api/notifications/unread-count"));
        Assert.Equal(0, await stranger.GetFromJsonAsync<int>("/api/notifications/unread-count"));
    }

    [Fact]
    public async Task Other_User_Cannot_Mark_Someone_Elses_Notification_Read()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.LejlaToken);
        await client.PostAsync($"/api/notifications/{world.NotificationId}/read", null);

        var stored = await TestWorld.FindNotificationAsync(factory, world.NotificationId);
        Assert.NotNull(stored);
        Assert.Null(stored.ReadAt);
    }

    [Fact]
    public async Task Marking_All_Read_Only_Touches_Your_Own_Notifications()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var stranger = TestWorld.ClientFor(factory, world.LejlaToken);
        await stranger.PostAsync("/api/notifications/read-all", null);

        var stored = await TestWorld.FindNotificationAsync(factory, world.NotificationId);
        Assert.NotNull(stored);
        Assert.Null(stored.ReadAt);
    }

    [Fact]
    public async Task Recipient_Can_Mark_Their_Notification_Read()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.MarkoToken);
        await client.PostAsync($"/api/notifications/{world.NotificationId}/read", null);

        var stored = await TestWorld.FindNotificationAsync(factory, world.NotificationId);
        Assert.NotNull(stored);
        Assert.NotNull(stored.ReadAt);

        Assert.Equal(0, await client.GetFromJsonAsync<int>("/api/notifications/unread-count"));
    }

    [Fact]
    public async Task Creating_An_Event_Notifies_The_Partner_But_Not_The_Creator()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var creator = TestWorld.ClientFor(factory, world.AnaToken);
        var request = new CreateEventRequest("Kino", null, DateTime.UtcNow.AddDays(3));
        var response = await creator.PostAsJsonAsync("/api/events", request);
        response.EnsureSuccessStatusCode();

        var partner = TestWorld.ClientFor(factory, world.MarkoToken);
        var partnerNotifications = await partner.GetFromJsonAsync<List<NotificationDto>>("/api/notifications");
        Assert.NotNull(partnerNotifications);
        Assert.Contains(partnerNotifications, n => n.EntityTitle == "Kino" && n.EntityType == "event");

        var creatorNotifications = await creator.GetFromJsonAsync<List<NotificationDto>>("/api/notifications");
        Assert.NotNull(creatorNotifications);
        Assert.DoesNotContain(creatorNotifications, n => n.EntityTitle == "Kino");
    }
}
