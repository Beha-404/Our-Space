using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OurSpace.API.Data;
using OurSpace.API.Services;
using Xunit;

namespace OurSpace.Tests;

public class DeleteOrderTests
{
    [Fact]
    public async Task Photo_Files_Are_Deleted_Only_After_The_Row_Is_Committed()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var spy = new DeletionSpy(() => RowExistsAsync(factory, db => db.Photos.AnyAsync(p => p.Id == world.PhotoId)));

        var client = ClientWith(factory, spy, world.AnaToken);
        var response = await client.DeleteAsync($"/api/photos/{world.PhotoId}");

        response.EnsureSuccessStatusCode();

        Assert.Equal(2, spy.RowExistedAtDelete.Count);
        Assert.All(spy.RowExistedAtDelete, existed => Assert.False(existed));
    }

    [Fact]
    public async Task Audio_Files_Are_Deleted_Only_After_The_Row_Is_Committed()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var spy = new DeletionSpy(() => RowExistsAsync(factory, db => db.AudioMessages.AnyAsync(a => a.Id == world.AudioId)));

        var client = ClientWith(factory, spy, world.AnaToken);
        var response = await client.DeleteAsync($"/api/audio/{world.AudioId}");

        response.EnsureSuccessStatusCode();

        Assert.Single(spy.RowExistedAtDelete);
        Assert.False(spy.RowExistedAtDelete[0]);
    }

    [Fact]
    public async Task Unpair_Deletes_Files_Only_After_The_Couple_Is_Gone()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var spy = new DeletionSpy(() => RowExistsAsync(factory, db => db.Photos.AnyAsync(p => p.Id == world.PhotoId)));

        var client = ClientWith(factory, spy, world.AnaToken);
        var response = await client.PostAsync("/api/user/unpair", null);

        response.EnsureSuccessStatusCode();

        Assert.Equal(3, spy.RowExistedAtDelete.Count);
        Assert.All(spy.RowExistedAtDelete, existed => Assert.False(existed));
    }

    [Fact]
    public async Task A_Rejected_Delete_Leaves_The_File_Untouched()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var spy = new DeletionSpy(() => Task.FromResult(true));

        var client = ClientWith(factory, spy, world.LejlaToken);
        var response = await client.DeleteAsync($"/api/photos/{world.PhotoId}");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(spy.RowExistedAtDelete);
        Assert.NotNull(await TestWorld.FindPhotoAsync(factory, world.PhotoId));
    }

    private static HttpClient ClientWith(OurSpaceFactory factory, IFileStorageService storage, string token)
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<IFileStorageService>();
                services.AddSingleton(storage);
            }))
            .CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private static async Task<bool> RowExistsAsync(OurSpaceFactory factory, Func<AppDbContext, Task<bool>> query)
    {
        using var scope = factory.Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private sealed class DeletionSpy(Func<Task<bool>> rowExists) : IFileStorageService
    {
        public List<bool> RowExistedAtDelete { get; } = [];

        public void Delete(string url) => RowExistedAtDelete.Add(rowExists().GetAwaiter().GetResult());

        public Task<string> SaveAsync(Stream content, string subfolder, string fileExtension) =>
            Task.FromResult($"/uploads/{subfolder}/spy{fileExtension}");

        public Task<Stream> OpenReadAsync(string url) => Task.FromResult<Stream>(new MemoryStream());

        public long GetSizeBytes(string url) => 0;
    }
}
