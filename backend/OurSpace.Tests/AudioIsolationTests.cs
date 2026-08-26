using System.Net;
using System.Net.Http.Json;
using OurSpace.API.Models.DTOs.Memory;
using Xunit;

namespace OurSpace.Tests;

public class AudioIsolationTests
{
    [Fact]
    public async Task Partner_Hears_Audio_From_Same_Couple()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.MarkoToken);
        var page = await client.GetFromJsonAsync<PagedResult<AudioDto>>("/api/audio");

        Assert.NotNull(page);
        Assert.Contains(page.Items, a => a.Caption == TestWorld.AudioCaption);
    }

    [Fact]
    public async Task Other_Couple_Does_Not_Hear_Audio()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.LejlaToken);
        var page = await client.GetFromJsonAsync<PagedResult<AudioDto>>("/api/audio");

        Assert.NotNull(page);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task Other_Couple_Cannot_Delete_Audio()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.LejlaToken);
        var response = await client.DeleteAsync($"/api/audio/{world.AudioId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(await TestWorld.FindAudioAsync(factory, world.AudioId));
    }
}
