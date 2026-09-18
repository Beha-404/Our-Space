using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Home;
using OurSpace.API.Models.Entities;
using Xunit;

namespace OurSpace.Tests;

public class PartnerActivityTests
{
    [Fact]
    public async Task Partner_Card_Counts_What_The_Partner_Added()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.MarkoToken);
        var activity = await client.GetFromJsonAsync<PartnerActivityDto>("/api/home/partner");

        Assert.NotNull(activity);
        Assert.Equal(1, activity.Photos);
        Assert.Equal(1, activity.VoiceLetters);
        Assert.Equal(1, activity.Wishes);
        Assert.Equal(1, activity.Events);
        Assert.Equal(2, activity.Capsules);
        var photo = Assert.Single(activity.RecentPhotos);
        Assert.Equal(world.PhotoId, photo.Id);
        Assert.Equal(TestWorld.PhotoCaption, photo.Caption);
        Assert.Contains("?", photo.ThumbnailUrl);
    }

    [Fact]
    public async Task Own_Items_Are_Not_Counted_For_The_Partner()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.AnaToken);
        var activity = await client.GetFromJsonAsync<PartnerActivityDto>("/api/home/partner");

        Assert.NotNull(activity);
        Assert.Equal(0, activity.Photos);
        Assert.Equal(0, activity.VoiceLetters);
        Assert.Equal(0, activity.Wishes);
        Assert.Equal(0, activity.Events);
        Assert.Equal(0, activity.Capsules);
        Assert.Empty(activity.RecentPhotos);
    }

    [Fact]
    public async Task Other_Couple_Sees_None_Of_It()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.LejlaToken);
        var activity = await client.GetFromJsonAsync<PartnerActivityDto>("/api/home/partner");

        Assert.NotNull(activity);
        Assert.Equal(0, activity.Photos + activity.VoiceLetters + activity.Wishes + activity.Events + activity.Capsules);
        Assert.Empty(activity.RecentPhotos);
    }

    [Fact]
    public async Task Recent_Photos_Are_The_Four_Newest()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);
        await AddPhotosAsync(factory, world.PhotoId, 5);

        var client = TestWorld.ClientFor(factory, world.MarkoToken);
        var activity = await client.GetFromJsonAsync<PartnerActivityDto>("/api/home/partner");

        Assert.NotNull(activity);
        Assert.Equal(6, activity.Photos);
        Assert.Equal(["Nova 5", "Nova 4", "Nova 3", "Nova 2"], activity.RecentPhotos.Select(p => p.Caption));
    }

    [Fact]
    public async Task User_Without_A_Partner_Gets_A_Bad_Request()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);
        var token = await TestWorld.UnpairedTokenAsync(factory, "sanja");

        var client = TestWorld.ClientFor(factory, token);
        var response = await client.GetAsync("/api/home/partner");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task AddPhotosAsync(OurSpaceFactory factory, int seedPhotoId, int count)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await db.Photos.FindAsync(seedPhotoId);

        for (var i = 1; i <= count; i++)
        {
            db.Photos.Add(new Photo
            {
                CoupleId = seed!.CoupleId,
                UploadedByUserId = seed.UploadedByUserId,
                FilePath = $"/uploads/photos/new-{i}.jpg",
                ThumbnailPath = $"/uploads/photos/new-{i}-thumb.jpg",
                Caption = $"Nova {i}",
                TakenAt = seed.TakenAt,
                CreatedAt = DateTime.UtcNow.AddMinutes(i),
            });
        }

        await db.SaveChangesAsync();
    }
}
