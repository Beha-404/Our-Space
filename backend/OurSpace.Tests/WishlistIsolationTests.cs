using System.Net;
using System.Net.Http.Json;
using OurSpace.API.Models.DTOs.Wishlist;
using Xunit;

namespace OurSpace.Tests;

public class WishlistIsolationTests
{
    [Fact]
    public async Task Partner_Sees_Wish_From_Same_Couple()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.MarkoToken);
        var wishes = await client.GetFromJsonAsync<List<WishDto>>("/api/wishlist");

        Assert.NotNull(wishes);
        Assert.Contains(wishes, w => w.Title == TestWorld.WishTitle);
    }

    [Fact]
    public async Task Other_Couple_Does_Not_See_Wish()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.LejlaToken);
        var wishes = await client.GetFromJsonAsync<List<WishDto>>("/api/wishlist");

        Assert.NotNull(wishes);
        Assert.Empty(wishes);
    }

    [Fact]
    public async Task Other_Couple_Cannot_Toggle_Wish()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.LejlaToken);
        var response = await client.PutAsync($"/api/wishlist/{world.WishId}/toggle", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var stored = await TestWorld.FindWishAsync(factory, world.WishId);
        Assert.NotNull(stored);
        Assert.False(stored.IsFulfilled);
    }

    [Fact]
    public async Task Other_Couple_Cannot_Delete_Wish()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.LejlaToken);
        var response = await client.DeleteAsync($"/api/wishlist/{world.WishId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(await TestWorld.FindWishAsync(factory, world.WishId));
    }
}
