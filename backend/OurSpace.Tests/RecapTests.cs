using System.Net.Http.Json;
using System.Text.Json;
using OurSpace.API.Models.DTOs.Recap;
using Xunit;

namespace OurSpace.Tests;

public class RecapTests
{
    [Fact]
    public async Task Recap_Counts_The_Couples_Own_Year()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.MarkoToken);
        var recap = await client.GetFromJsonAsync<RecapDto>("/api/recap");

        Assert.NotNull(recap);
        Assert.Equal(DateTime.UtcNow.Year, recap.Year);
        Assert.Equal(1, recap.Photos);
        Assert.Equal(1, recap.AudioMessages);
        Assert.Equal(2, recap.CapsulesSealed);
        Assert.Equal(12, recap.MemoriesPerMonth.Count);
        Assert.Equal(2, recap.MemoriesPerMonth[DateTime.UtcNow.Month - 1]);
        Assert.Single(recap.Highlights);
    }

    [Fact]
    public async Task Recap_Never_Counts_Another_Couples_Memories()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.LejlaToken);
        var recap = await client.GetFromJsonAsync<RecapDto>("/api/recap");

        Assert.NotNull(recap);
        Assert.Equal(0, recap.Photos);
        Assert.Equal(0, recap.AudioMessages);
        Assert.Equal(0, recap.Events);
        Assert.Equal(0, recap.CapsulesSealed);
        Assert.Empty(recap.Highlights);
        Assert.All(recap.MemoriesPerMonth, count => Assert.Equal(0, count));
    }

    [Fact]
    public async Task Recap_For_An_Empty_Year_Returns_Zeros()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.MarkoToken);
        var recap = await client.GetFromJsonAsync<RecapDto>("/api/recap?year=1999");

        Assert.NotNull(recap);
        Assert.Equal(1999, recap.Year);
        Assert.Equal(0, recap.Photos);
        Assert.Empty(recap.Highlights);
        Assert.Contains(DateTime.UtcNow.Year, recap.AvailableYears);
    }

    [Fact]
    public async Task Home_Teaser_Sums_Up_This_Year_With_Signed_Photos()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.MarkoToken);
        var teaser = await GetTeaserAsync(client);

        Assert.Equal(DateTime.UtcNow.Year, teaser.GetProperty("year").GetInt32());
        Assert.Equal(2, teaser.GetProperty("memories").GetInt32());
        Assert.Equal(DateTime.UtcNow.Month, teaser.GetProperty("busiestMonth").GetInt32());

        var photo = Assert.Single(teaser.GetProperty("photoUrls").EnumerateArray());
        Assert.Contains("?", photo.GetString());
    }

    [Fact]
    public async Task Home_Teaser_Of_Another_Couple_Is_Empty()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.LejlaToken);
        var teaser = await GetTeaserAsync(client);

        Assert.Equal(0, teaser.GetProperty("memories").GetInt32());
        Assert.Equal(JsonValueKind.Null, teaser.GetProperty("busiestMonth").ValueKind);
        Assert.Empty(teaser.GetProperty("photoUrls").EnumerateArray());
    }

    private static async Task<JsonElement> GetTeaserAsync(HttpClient client)
    {
        var home = await client.GetFromJsonAsync<JsonElement>("/api/home");
        return home.GetProperty("yearTeaser");
    }
}
