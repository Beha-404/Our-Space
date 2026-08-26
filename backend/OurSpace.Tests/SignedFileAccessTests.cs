using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OurSpace.API.Models.DTOs.Memory;
using OurSpace.API.Services;
using Xunit;

namespace OurSpace.Tests;

public class SignedFileAccessTests
{
    private const string FilePath = "/uploads/photos/seed.jpg";

    [Fact]
    public async Task Unsigned_Upload_Url_Is_Rejected()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var response = await factory.CreateClient().GetAsync(FilePath);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Tampered_Signature_Is_Rejected()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var signed = Signer(factory).Sign(FilePath);
        var tampered = signed[..^2] + (signed.EndsWith("aa") ? "bb" : "aa");

        var response = await factory.CreateClient().GetAsync(tampered);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Signature_For_Another_File_Is_Rejected()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var signed = Signer(factory).Sign(FilePath);
        var query = signed[signed.IndexOf('?')..];

        var response = await factory.CreateClient().GetAsync("/uploads/photos/other.jpg" + query);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Expired_Signature_Is_Rejected()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var expired = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds().ToString();
        var signer = Signer(factory);
        var stale = $"{FilePath}?exp={expired}&sig=whatever";

        Assert.False(signer.IsValid(FilePath, expired, "whatever"));

        var response = await factory.CreateClient().GetAsync(stale);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Fresh_Signature_Passes_The_Guard()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var signed = Signer(factory).Sign(FilePath);
        var separator = signed.IndexOf('?');
        var query = System.Web.HttpUtility.ParseQueryString(signed[(separator + 1)..]);

        Assert.True(Signer(factory).IsValid(FilePath, query["exp"], query["sig"]));
    }

    [Fact]
    public async Task Same_File_Signs_To_A_Stable_Url_So_Browsers_Can_Cache()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var signer = Signer(factory);

        Assert.Equal(signer.Sign(FilePath), signer.Sign(FilePath));
    }

    [Fact]
    public async Task Photo_Urls_From_Api_Are_Signed()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.AnaToken);
        var page = await client.GetFromJsonAsync<PagedResult<PhotoDto>>("/api/photos");

        var photo = Assert.Single(page!.Items);

        Assert.Contains("?exp=", photo.Url);
        Assert.Contains("&sig=", photo.Url);
        Assert.Contains("&sig=", photo.ThumbnailUrl);
    }

    [Fact]
    public async Task Audio_Urls_From_Api_Are_Signed()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.AnaToken);
        var page = await client.GetFromJsonAsync<PagedResult<AudioDto>>("/api/audio");

        var audio = Assert.Single(page!.Items);

        Assert.Contains("?exp=", audio.Url);
        Assert.Contains("&sig=", audio.Url);
    }

    private static IFileUrlSigner Signer(OurSpaceFactory factory) =>
        factory.Services.GetRequiredService<IFileUrlSigner>();
}
