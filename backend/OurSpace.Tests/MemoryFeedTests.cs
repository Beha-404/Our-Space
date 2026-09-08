using System.Net.Http.Json;
using OurSpace.API.Models.DTOs.Memory;
using Xunit;

namespace OurSpace.Tests;

public class MemoryFeedTests
{
    [Fact]
    public async Task Feed_Returns_Photos_And_Audio_In_One_List()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.AnaToken);
        var feed = await client.GetFromJsonAsync<MemoryFeedDto>("/api/memories");

        Assert.NotNull(feed);
        Assert.Contains(feed.Items, m => m.Type == "photo" && m.Caption == TestWorld.PhotoCaption);
        Assert.Contains(feed.Items, m => m.Type == "audio" && m.Caption == TestWorld.AudioCaption);
    }

    [Fact]
    public async Task Every_Row_Reports_A_Status()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.AnaToken);
        var feed = await client.GetFromJsonAsync<MemoryFeedDto>("/api/memories");

        Assert.NotNull(feed);
        Assert.NotEmpty(feed.Items);
        Assert.All(feed.Items, m => Assert.False(string.IsNullOrWhiteSpace(m.Status)));
    }

    [Fact]
    public async Task Type_Filter_Narrows_The_Feed()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.AnaToken);
        var photosOnly = await client.GetFromJsonAsync<MemoryFeedDto>("/api/memories?type=photo");

        Assert.NotNull(photosOnly);
        Assert.NotEmpty(photosOnly.Items);
        Assert.All(photosOnly.Items, m => Assert.Equal("photo", m.Type));
    }

    [Fact]
    public async Task Other_Couple_Sees_Nothing()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.LejlaToken);
        var feed = await client.GetFromJsonAsync<MemoryFeedDto>("/api/memories");

        Assert.NotNull(feed);
        Assert.Empty(feed.Items);
    }
}
