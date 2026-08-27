using Microsoft.Extensions.DependencyInjection;
using OurSpace.API.Services;
using Xunit;

namespace OurSpace.Tests;

public class FileDownloadTests
{
    private const string FilePath = "/uploads/photos/seed.jpg";

    [Fact]
    public async Task Download_Flag_Sets_Attachment_Header()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var signed = Signer(factory).Sign(FilePath);
        var response = await factory.CreateClient().GetAsync(signed + "&download=1");

        Assert.Equal("attachment; filename=\"seed.jpg\"", response.Content.Headers.ContentDisposition?.ToString());
    }

    [Fact]
    public async Task Download_Uses_A_Sanitised_Custom_Name()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var signed = Signer(factory).Sign(FilePath);
        var response = await factory.CreateClient().GetAsync(signed + "&download=1&name=Piknik%20na%20zalasku");

        Assert.Equal("attachment; filename=\"Piknik na zalasku.jpg\"", response.Content.Headers.ContentDisposition?.ToString());
    }

    [Fact]
    public async Task Download_Name_Cannot_Inject_Header_Content()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var signed = Signer(factory).Sign(FilePath);
        var response = await factory.CreateClient().GetAsync(signed + "&download=1&name=a%22%0d%0aX-Evil:%20yes");

        var disposition = response.Content.Headers.ContentDisposition?.ToString();

        Assert.NotNull(disposition);
        Assert.DoesNotContain("\r", disposition);
        Assert.DoesNotContain("\n", disposition);
        Assert.Equal(2, disposition.Count(c => c == '"'));
        Assert.False(response.Headers.Contains("X-Evil"));
    }

    [Fact]
    public async Task Download_Still_Requires_A_Valid_Signature()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var response = await factory.CreateClient().GetAsync(FilePath + "?download=1");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    private static IFileUrlSigner Signer(OurSpaceFactory factory) =>
        factory.Services.GetRequiredService<IFileUrlSigner>();
}
