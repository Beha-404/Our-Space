using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace OurSpace.Tests;

public class TextLengthLimitTests
{
    [Fact]
    public async Task Registering_With_An_Oversized_Username_Is_A_Clean_400()
    {
        using var factory = new OurSpaceFactory(registrationOpen: true);
        await TestWorld.SeedAsync(factory);

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            username = new string('a', 31),
            email = "oversized@example.com",
            password = "ValidnaSifra123",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_Username_At_Exactly_The_Limit_Is_Accepted()
    {
        using var factory = new OurSpaceFactory(registrationOpen: true);
        await TestWorld.SeedAsync(factory);

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            username = new string('a', 30),
            email = "exactlimit@example.com",
            password = "ValidnaSifra123",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Registering_With_An_Oversized_Email_Is_A_Clean_400()
    {
        using var factory = new OurSpaceFactory(registrationOpen: true);
        await TestWorld.SeedAsync(factory);

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            username = "shortname",
            email = new string('a', 250) + "@a.co",
            password = "ValidnaSifra123",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Registering_With_An_Oversized_Password_Is_A_Clean_400_Not_A_Slow_Hash()
    {
        using var factory = new OurSpaceFactory(registrationOpen: true);
        await TestWorld.SeedAsync(factory);

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            username = "shortname2",
            email = "shortname2@example.com",
            password = "Aa1" + new string('a', 200),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Renaming_To_An_Oversized_Username_Is_A_Clean_400()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var response = await TestWorld.ClientFor(factory, world.AnaToken)
            .PutAsJsonAsync("/api/user", new { username = new string('a', 31) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Requesting_An_Oversized_New_Email_Is_A_Clean_400()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var response = await TestWorld.ClientFor(factory, world.AnaToken)
            .PostAsJsonAsync("/api/user/email-change", new { newEmail = new string('a', 250) + "@a.co" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_Oversized_Event_Title_Is_A_Clean_400()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var response = await TestWorld.ClientFor(factory, world.AnaToken)
            .PostAsJsonAsync("/api/events", new { title = new string('a', 201), description = (string?)null, eventDate = DateTime.UtcNow });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_Oversized_Event_Description_Is_A_Clean_400()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var response = await TestWorld.ClientFor(factory, world.AnaToken)
            .PostAsJsonAsync("/api/events", new { title = "Kratak naslov", description = new string('a', 2001), eventDate = DateTime.UtcNow });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_Oversized_Wish_Title_Is_A_Clean_400()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var response = await TestWorld.ClientFor(factory, world.AnaToken)
            .PostAsJsonAsync("/api/wishlist", new { title = new string('a', 201) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_Oversized_Photo_Caption_Is_A_Clean_400()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.AnaToken);
        using var content = new MultipartFormDataContent
        {
            { new StringContent(new string('a', 301)), "caption" },
            { new StringContent(DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")), "takenAt" },
        };
        content.Add(FileContent(), "file", "x.jpg");

        var response = await client.PostAsync("/api/photos", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_Oversized_Audio_Caption_Is_A_Clean_400()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.AnaToken);
        using var content = new MultipartFormDataContent
        {
            { new StringContent(new string('a', 301)), "caption" },
            { new StringContent(DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")), "recordedAt" },
        };
        var file = new ByteArrayContent(new byte[] { 1, 2, 3 });
        file.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
        content.Add(file, "file", "x.mp3");

        var response = await client.PostAsync("/api/audio", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static ByteArrayContent FileContent()
    {
        using var image = new Image<Rgba32>(20, 20);
        using var buffer = new MemoryStream();
        image.SaveAsPng(buffer);

        var file = new ByteArrayContent(buffer.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        return file;
    }
}
