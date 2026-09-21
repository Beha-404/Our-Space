using System.Net;
using System.Net.Http.Json;
using OurSpace.API.Models.DTOs.Event;
using OurSpace.API.Models.DTOs.Notification;
using Xunit;

namespace OurSpace.Tests;

public class EventCancellationTests
{
    [Fact]
    public async Task Cancelling_Marks_The_Event_And_Keeps_It_In_The_Full_List()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);
        var client = TestWorld.ClientFor(factory, world.AnaToken);

        var response = await client.PostAsync($"/api/events/{world.EventId}/cancel", null);
        var dto = await response.Content.ReadFromJsonAsync<EventDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(dto!.IsCancelled);

        var all = await client.GetFromJsonAsync<List<EventDto>>("/api/events?includePast=true");
        var stored = Assert.Single(all!, e => e.Id == world.EventId);
        Assert.True(stored.IsCancelled);
    }

    [Fact]
    public async Task A_Cancelled_Upcoming_Event_Stays_In_The_Upcoming_List_Marked_As_Cancelled()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);
        var client = TestWorld.ClientFor(factory, world.AnaToken);

        await client.PostAsync($"/api/events/{world.EventId}/cancel", null);

        var upcoming = await client.GetFromJsonAsync<List<EventDto>>("/api/events");
        var stored = Assert.Single(upcoming!, e => e.Id == world.EventId);
        Assert.True(stored.IsCancelled);
    }

    [Fact]
    public async Task Either_Partner_Can_Cancel_And_Restore()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);
        var creator = TestWorld.ClientFor(factory, world.AnaToken);
        var partner = TestWorld.ClientFor(factory, world.MarkoToken);

        (await partner.PostAsync($"/api/events/{world.EventId}/cancel", null)).EnsureSuccessStatusCode();
        var restored = await creator.PostAsync($"/api/events/{world.EventId}/restore", null);
        var dto = await restored.Content.ReadFromJsonAsync<EventDto>();

        Assert.False(dto!.IsCancelled);
        var upcoming = await creator.GetFromJsonAsync<List<EventDto>>("/api/events");
        Assert.Contains(upcoming!, e => e.Id == world.EventId);
    }

    [Fact]
    public async Task Cancelling_Tells_The_Partner_But_Not_The_Person_Who_Cancelled()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);
        var creator = TestWorld.ClientFor(factory, world.AnaToken);
        var partner = TestWorld.ClientFor(factory, world.MarkoToken);

        await creator.PostAsync($"/api/events/{world.EventId}/cancel", null);

        var forPartner = await partner.GetFromJsonAsync<List<NotificationDto>>("/api/notifications");
        var forCreator = await creator.GetFromJsonAsync<List<NotificationDto>>("/api/notifications");

        Assert.Contains(forPartner!, n => n.Type == "EventCancelled" && n.EntityId == world.EventId);
        Assert.DoesNotContain(forCreator!, n => n.Type == "EventCancelled");
    }

    [Fact]
    public async Task Restoring_Tells_The_Partner()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);
        var creator = TestWorld.ClientFor(factory, world.AnaToken);
        var partner = TestWorld.ClientFor(factory, world.MarkoToken);

        await creator.PostAsync($"/api/events/{world.EventId}/cancel", null);
        await creator.PostAsync($"/api/events/{world.EventId}/restore", null);

        var forPartner = await partner.GetFromJsonAsync<List<NotificationDto>>("/api/notifications");
        Assert.Contains(forPartner!, n => n.Type == "EventRestored");
    }

    [Fact]
    public async Task Cancelling_Twice_Only_Notifies_Once()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);
        var creator = TestWorld.ClientFor(factory, world.AnaToken);
        var partner = TestWorld.ClientFor(factory, world.MarkoToken);

        await creator.PostAsync($"/api/events/{world.EventId}/cancel", null);
        var again = await creator.PostAsync($"/api/events/{world.EventId}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        var forPartner = await partner.GetFromJsonAsync<List<NotificationDto>>("/api/notifications");
        Assert.Single(forPartner!, n => n.Type == "EventCancelled");
    }

    [Fact]
    public async Task A_Cancelled_Event_Cannot_Be_Edited_Until_Restored()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);
        var client = TestWorld.ClientFor(factory, world.AnaToken);
        var edit = new CreateEventRequest("Novi naziv", null, DateTime.UtcNow.AddDays(20));

        await client.PostAsync($"/api/events/{world.EventId}/cancel", null);
        var blocked = await client.PutAsJsonAsync($"/api/events/{world.EventId}", edit);

        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        Assert.Equal(TestWorld.EventTitle, (await TestWorld.FindEventAsync(factory, world.EventId))!.Title);

        await client.PostAsync($"/api/events/{world.EventId}/restore", null);
        var allowed = await client.PutAsJsonAsync($"/api/events/{world.EventId}", edit);

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Events_Can_No_Longer_Be_Deleted()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);
        var client = TestWorld.ClientFor(factory, world.AnaToken);

        var response = await client.DeleteAsync($"/api/events/{world.EventId}");

        Assert.False(response.IsSuccessStatusCode);
        Assert.NotNull(await TestWorld.FindEventAsync(factory, world.EventId));
    }

    [Fact]
    public async Task Other_Couple_Cannot_Restore_Event()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);
        var owner = TestWorld.ClientFor(factory, world.AnaToken);
        var stranger = TestWorld.ClientFor(factory, world.LejlaToken);

        await owner.PostAsync($"/api/events/{world.EventId}/cancel", null);
        var response = await stranger.PostAsync($"/api/events/{world.EventId}/restore", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull((await TestWorld.FindEventAsync(factory, world.EventId))!.CancelledAt);
    }
}
