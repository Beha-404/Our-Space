using System.Net;
using System.Net.Http.Json;
using OurSpace.API.Models.DTOs.Event;
using Xunit;

namespace OurSpace.Tests;

public class EventIsolationTests
{
    [Fact]
    public async Task Partner_Sees_Event_From_Same_Couple()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.MarkoToken);
        var events = await client.GetFromJsonAsync<List<EventDto>>("/api/events");

        Assert.NotNull(events);
        Assert.Contains(events, e => e.Title == TestWorld.EventTitle);
    }

    [Fact]
    public async Task Other_Couple_Does_Not_See_Event()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.LejlaToken);
        var events = await client.GetFromJsonAsync<List<EventDto>>("/api/events");

        Assert.NotNull(events);
        Assert.Empty(events);
    }

    [Fact]
    public async Task Other_Couple_Cannot_Update_Event()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.LejlaToken);
        var response = await client.PutAsJsonAsync(
            $"/api/events/{world.EventId}",
            new CreateEventRequest("Hakovano", null, DateTime.UtcNow.AddDays(1)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var stored = await TestWorld.FindEventAsync(factory, world.EventId);
        Assert.NotNull(stored);
        Assert.Equal(TestWorld.EventTitle, stored.Title);
    }

    [Fact]
    public async Task Other_Couple_Cannot_Cancel_Event()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.LejlaToken);
        var response = await client.PostAsync($"/api/events/{world.EventId}/cancel", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null((await TestWorld.FindEventAsync(factory, world.EventId))!.CancelledAt);
    }

    [Fact]
    public async Task Missing_And_Foreign_Event_Look_Identical()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.LejlaToken);

        var foreign = await client.PostAsync($"/api/events/{world.EventId}/cancel", null);
        var missing = await client.PostAsync("/api/events/999999/cancel", null);

        Assert.Equal(missing.StatusCode, foreign.StatusCode);
        Assert.Equal(
            await missing.Content.ReadAsStringAsync(),
            await foreign.Content.ReadAsStringAsync());
    }
}
