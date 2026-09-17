using System.Net.Http.Json;
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
}
