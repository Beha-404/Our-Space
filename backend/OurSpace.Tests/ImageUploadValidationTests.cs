using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OurSpace.API.Models.DTOs.Memory;
using OurSpace.API.Models.DTOs.User;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace OurSpace.Tests;

public class ImageUploadValidationTests
{
    [Fact]
    public async Task Photo_Upload_Rejects_A_Non_Image_Claiming_To_Be_A_Jpeg()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.AnaToken);
        var payload = Encoding.UTF8.GetBytes("<h1>not an image</h1>");

        var response = await client.PostAsync("/api/photos", PhotoForm(payload, "photo.html", "image/jpeg"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Photo_Upload_Ignores_The_Client_Supplied_Extension()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.AnaToken);

        var response = await client.PostAsync("/api/photos", PhotoForm(Png(), "photo.html", "image/jpeg"));
        response.EnsureSuccessStatusCode();

        var photo = await response.Content.ReadFromJsonAsync<PhotoDto>();

        Assert.StartsWith("/uploads/photos/", photo!.Url);
        Assert.Contains(".png?", photo.Url);
        Assert.DoesNotContain(".html", photo.Url);

        await client.DeleteAsync($"/api/photos/{photo.Id}");
    }

    [Fact]
    public async Task Avatar_Upload_Rejects_A_Non_Image_Claiming_To_Be_A_Jpeg()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.AnaToken);
        var payload = Encoding.UTF8.GetBytes("<script>alert(1)</script>");

        var response = await client.PostAsync("/api/user/profile-picture", AvatarForm(payload, "avatar.html", "image/jpeg"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Avatar_Upload_Ignores_The_Client_Supplied_Extension()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.AnaToken);

        var response = await client.PostAsync("/api/user/profile-picture", AvatarForm(Png(), "avatar.html", "image/jpeg"));
        response.EnsureSuccessStatusCode();

        var user = await response.Content.ReadFromJsonAsync<UserDto>();

        Assert.Contains(".webp?", user!.ProfilePictureUrl);
        Assert.DoesNotContain(".html", user.ProfilePictureUrl);

        DeleteStoredFile(factory, user.ProfilePictureUrl!);
    }

    private static void DeleteStoredFile(OurSpaceFactory factory, string signedUrl)
    {
        var root = factory.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        var relative = signedUrl.Split('?')[0].TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

        File.Delete(Path.Combine(root, relative));
    }

    private static MultipartFormDataContent PhotoForm(byte[] bytes, string fileName, string contentType)
    {
        var form = AvatarForm(bytes, fileName, contentType);
        form.Add(new StringContent(DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")), "takenAt");
        form.Add(new StringContent("Test"), "caption");

        return form;
    }

    private static MultipartFormDataContent AvatarForm(byte[] bytes, string fileName, string contentType)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    private static byte[] Png()
    {
        using var image = new Image<Rgba32>(40, 40);
        using var buffer = new MemoryStream();

        image.SaveAsPng(buffer);

        return buffer.ToArray();
    }
}
