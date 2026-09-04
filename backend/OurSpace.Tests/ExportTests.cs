using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OurSpace.API.Data;
using OurSpace.API.Models.Entities;
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
    public async Task A_Large_Library_Is_Handed_To_A_Background_Job_Instead_Of_Streaming()
    {
        using var factory = new OurSpaceFactory(exportInlineLimitBytes: 1);
        var world = await TestWorld.SeedAsync(factory);

        await GiveTheCoupleSomeWeightAsync(factory);

        var client = ClientWith(factory, world.MarkoToken);
        var response = await client.GetAsync("/api/export/memories");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var job = await db.BackgroundJobs.SingleAsync();
        Assert.Equal(BackgroundJobTypes.MemoriesExport, job.Type);
        Assert.Equal(BackgroundJobStatus.Pending, job.Status);
    }

    private static async Task GiveTheCoupleSomeWeightAsync(OurSpaceFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var photo = await db.Photos.FirstAsync();
        photo.SizeBytes = 5_000_000;
        await db.SaveChangesAsync();
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
