using System.Net;
using System.Net.Http.Json;
using OurSpace.API.Models.DTOs.Memory;
using Xunit;

namespace OurSpace.Tests;

public class PhotoIsolationTests
{
    [Fact]
    public async Task Partner_Sees_Photo_From_Same_Couple()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.MarkoToken);
        var page = await client.GetFromJsonAsync<PagedResult<PhotoDto>>("/api/photos");

        Assert.NotNull(page);
        Assert.Contains(page.Items, p => p.Caption == TestWorld.PhotoCaption);
    }

    [Fact]
    public async Task Other_Couple_Does_Not_See_Photo()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.LejlaToken);
        var page = await client.GetFromJsonAsync<PagedResult<PhotoDto>>("/api/photos");

        Assert.NotNull(page);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task Other_Couple_Cannot_Delete_Photo()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.LejlaToken);
        var response = await client.DeleteAsync($"/api/photos/{world.PhotoId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(await TestWorld.FindPhotoAsync(factory, world.PhotoId));
    }
}
