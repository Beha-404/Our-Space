using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OurSpace.API.Services;
using Xunit;

namespace OurSpace.Tests;

public class ExportTests
{
    [Fact]
    public async Task Export_Returns_A_Zip_With_Both_Files()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = ClientWith(factory, world.MarkoToken);
        var response = await client.GetAsync("/api/export/memories");

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        Assert.Equal(2, archive.Entries.Count);
        Assert.Contains(archive.Entries, e => e.FullName.StartsWith("Slike/"));
        Assert.Contains(archive.Entries, e => e.FullName.StartsWith("Audio poruke/"));
    }

    [Fact]
    public async Task Export_Fails_Cleanly_When_There_Is_Nothing_To_Export()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = ClientWith(factory, world.LejlaToken);
        var response = await client.GetAsync("/api/export/memories");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static HttpClient ClientWith(OurSpaceFactory factory, string token)
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<IFileStorageService>();
                services.AddSingleton<IFileStorageService>(new FakeFileStorageService());
            }))
            .CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public Task<StoredFile> SaveAsync(Stream content, string subfolder, string fileExtension) =>
            Task.FromResult(new StoredFile($"/uploads/{subfolder}/fake{fileExtension}", 3));

        public Task<Stream> OpenReadAsync(string url) =>
            Task.FromResult<Stream>(new MemoryStream([1, 2, 3]));

        public Task DeleteAsync(string url) => Task.CompletedTask;
    }
}
