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
        Assert.Equal(0, feed.TotalCount);
    }

    [Fact]
    public async Task Feed_Is_Split_Into_Pages_And_Reports_The_Total()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.AnaToken);
        var first = await client.GetFromJsonAsync<MemoryFeedDto>("/api/memories?page=1&pageSize=1");
        var second = await client.GetFromJsonAsync<MemoryFeedDto>("/api/memories?page=2&pageSize=1");

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Single(first.Items);
        Assert.Single(second.Items);
        Assert.True(first.HasMore);
        Assert.False(second.HasMore);
        Assert.Equal(2, first.TotalCount);
        Assert.Equal(2, second.TotalCount);
        Assert.NotEqual((first.Items[0].Type, first.Items[0].Id), (second.Items[0].Type, second.Items[0].Id));
    }

    [Fact]
    public async Task Total_Follows_The_Type_Filter()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.AnaToken);
        var photos = await client.GetFromJsonAsync<MemoryFeedDto>("/api/memories?type=photo");

        Assert.Equal(1, photos!.TotalCount);
    }
}
