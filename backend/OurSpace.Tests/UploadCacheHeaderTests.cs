using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OurSpace.API.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace OurSpace.Tests;

public class UploadCacheHeaderTests
{
    [Fact]
    public async Task Served_Uploads_Carry_A_Long_Lived_Cache_Header()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        await WithFileOnDiskAsync(factory, async relativeUrl =>
        {
            var response = await factory.CreateClient().GetAsync(Signer(factory).Sign(relativeUrl));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.CacheControl!.Public);
            Assert.Equal(TimeSpan.FromHours(1), response.Headers.CacheControl.MaxAge);
        });
    }

    [Fact]
    public async Task Cache_Lifetime_Never_Outlives_The_Signature()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        await WithFileOnDiskAsync(factory, async relativeUrl =>
        {
            var signed = Signer(factory).Sign(relativeUrl);
            var query = System.Web.HttpUtility.ParseQueryString(signed[(signed.IndexOf('?') + 1)..]);
            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(query["exp"]!));

            var response = await factory.CreateClient().GetAsync(signed);
            var maxAge = response.Headers.CacheControl!.MaxAge!.Value;

            Assert.True(DateTimeOffset.UtcNow.Add(maxAge) <= expiresAt);
        });
    }

    [Fact]
    public async Task Served_Uploads_Are_Not_Content_Sniffed()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        await WithFileOnDiskAsync(factory, async relativeUrl =>
        {
            var response = await factory.CreateClient().GetAsync(Signer(factory).Sign(relativeUrl));

            Assert.Contains("nosniff", response.Headers.GetValues("X-Content-Type-Options"));
        });
    }

    private static async Task WithFileOnDiskAsync(OurSpaceFactory factory, Func<string, Task> assert)
    {
        var fileName = $"cache-header-test-{Guid.NewGuid():N}.png";
        var root = factory.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        var folder = Path.Combine(root, "uploads", "photos");
        var physicalPath = Path.Combine(folder, fileName);

        Directory.CreateDirectory(folder);

        using (var image = new Image<Rgba32>(20, 20))
            await image.SaveAsPngAsync(physicalPath);

        try
        {
            await assert($"/uploads/photos/{fileName}");
        }
        finally
        {
            File.Delete(physicalPath);
        }
    }

    private static IFileUrlSigner Signer(OurSpaceFactory factory) =>
        factory.Services.GetRequiredService<IFileUrlSigner>();
}
