using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Memory;
using OurSpace.API.Models.Entities;
using OurSpace.API.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace OurSpace.Tests;

public class MemoryMapTests
{
    private const string LocatedCaption = "Baščaršija";

    [Fact]
    public async Task Map_Shows_Only_Photos_With_A_Location()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);
        await AddLocatedPhotoAsync(factory, world.PhotoId);

        var client = TestWorld.ClientFor(factory, world.AnaToken);
        var map = await client.GetFromJsonAsync<MemoryMapDto>("/api/memories/map");

        Assert.NotNull(map);
        var point = Assert.Single(map.Points);
        Assert.Equal(LocatedCaption, point.Caption);
        Assert.Equal(43.8598, point.Latitude);
        Assert.Equal(18.4313, point.Longitude);
        Assert.Contains("?", point.ThumbnailUrl);
        Assert.Equal(1, map.PhotosWithoutLocation);
    }

    [Fact]
    public async Task Other_Couple_Does_Not_See_The_Map_Points()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);
        await AddLocatedPhotoAsync(factory, world.PhotoId);

        var client = TestWorld.ClientFor(factory, world.LejlaToken);
        var map = await client.GetFromJsonAsync<MemoryMapDto>("/api/memories/map");

        Assert.NotNull(map);
        Assert.Empty(map.Points);
        Assert.Equal(0, map.PhotosWithoutLocation);
    }

    [Fact]
    public async Task Uploaded_Photo_Keeps_The_Gps_Location_From_Its_Metadata()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.AnaToken);
        var file = new ByteArrayContent(JpegWithGps(43, 51, 35.28, "N", 18, 25, 52.68, "E"));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        var form = new MultipartFormDataContent
        {
            { file, "file", "trip.jpg" },
            { new StringContent(DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")), "takenAt" },
            { new StringContent("Izlet"), "caption" },
        };

        var response = await client.PostAsync("/api/photos", form);
        response.EnsureSuccessStatusCode();
        var uploaded = await response.Content.ReadFromJsonAsync<PhotoDto>();

        var stored = await TestWorld.FindPhotoAsync(factory, uploaded!.Id);
        Assert.Equal(43.8598, stored!.Latitude);
        Assert.Equal(18.4313, stored.Longitude);

        await client.DeleteAsync($"/api/photos/{uploaded.Id}");
    }

    [Fact]
    public void Southern_And_Western_Coordinates_Are_Negative()
    {
        using var image = Image.Load(JpegWithGps(33, 52, 4.0, "S", 151, 12, 36.0, "W"));

        var location = PhotoLocation.Read(image);

        Assert.NotNull(location);
        Assert.Equal(-33.8678, location.Value.Latitude);
        Assert.Equal(-151.21, location.Value.Longitude);
    }

    [Fact]
    public void Photo_Without_Gps_Has_No_Location()
    {
        using var image = new Image<Rgba32>(10, 10);

        Assert.Null(PhotoLocation.Read(image));
    }

    [Fact]
    public void Zero_Zero_Coordinates_Are_Treated_As_Missing()
    {
        using var image = Image.Load(JpegWithGps(0, 0, 0, "N", 0, 0, 0, "E"));

        Assert.Null(PhotoLocation.Read(image));
    }

    private static async Task AddLocatedPhotoAsync(OurSpaceFactory factory, int seededPhotoId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeded = await db.Photos.FindAsync(seededPhotoId);

        db.Photos.Add(new Photo
        {
            CoupleId = seeded!.CoupleId,
            UploadedByUserId = seeded.UploadedByUserId,
            FilePath = "/uploads/photos/located.jpg",
            ThumbnailPath = "/uploads/photos/located-thumb.jpg",
            Caption = LocatedCaption,
            TakenAt = seeded.TakenAt,
            Latitude = 43.8598,
            Longitude = 18.4313,
        });

        await db.SaveChangesAsync();
    }

    private static byte[] JpegWithGps(
        double latDeg, double latMin, double latSec, string latRef,
        double lonDeg, double lonMin, double lonSec, string lonRef)
    {
        using var image = new Image<Rgba32>(40, 40);
        var exif = new ExifProfile();

        exif.SetValue(ExifTag.GPSLatitude, [Rational.FromDouble(latDeg), Rational.FromDouble(latMin), new Rational(latSec)]);
        exif.SetValue(ExifTag.GPSLatitudeRef, latRef);
        exif.SetValue(ExifTag.GPSLongitude, [Rational.FromDouble(lonDeg), Rational.FromDouble(lonMin), new Rational(lonSec)]);
        exif.SetValue(ExifTag.GPSLongitudeRef, lonRef);
        image.Metadata.ExifProfile = exif;

        using var buffer = new MemoryStream();
        image.SaveAsJpeg(buffer);

        return buffer.ToArray();
    }
}
