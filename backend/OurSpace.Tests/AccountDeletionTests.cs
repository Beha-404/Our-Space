using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OurSpace.API.Data;
using OurSpace.API.Models.Entities;
using OurSpace.API.Services;
using Xunit;

namespace OurSpace.Tests;

public class AccountDeletionTests
{
    [Fact]
    public async Task Deleting_An_Account_Removes_The_Couples_Stored_Files()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var spy = new RecordingFileStorage();
        var client = ClientWith(factory, spy, world.AnaToken);

        var userId = await UserIdAsync(factory, "ana");
        var response = await client.DeleteAsync($"/api/user/{userId}");

        response.EnsureSuccessStatusCode();

        Assert.Contains("/uploads/photos/seed.jpg", spy.Deleted);
        Assert.Contains("/uploads/photos/seed-thumb.jpg", spy.Deleted);
        Assert.Contains("/uploads/audio/seed.webm", spy.Deleted);
    }

    [Fact]
    public async Task Username_And_Email_Are_Free_To_Reuse_After_Deletion()
    {
        using var factory = new OurSpaceFactory(registrationOpen: true);
        var world = await TestWorld.SeedAsync(factory);

        var client = ClientWith(factory, new RecordingFileStorage(), world.AnaToken);
        var userId = await UserIdAsync(factory, "ana");
        var email = await EmailAsync(factory, userId);

        (await client.DeleteAsync($"/api/user/{userId}")).EnsureSuccessStatusCode();

        var registerResponse = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { username = "ana", email, password = "NovaSifra123" });

        Assert.Equal(System.Net.HttpStatusCode.Created, registerResponse.StatusCode);
    }

    [Fact]
    public async Task Deleting_An_Account_Removes_The_Couple_And_Its_Content()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = ClientWith(factory, new RecordingFileStorage(), world.AnaToken);
        var userId = await UserIdAsync(factory, "ana");

        (await client.DeleteAsync($"/api/user/{userId}")).EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await db.Photos.AnyAsync(p => p.Id == world.PhotoId));
        Assert.False(await db.AudioMessages.AnyAsync(a => a.Id == world.AudioId));
        Assert.False(await db.Events.AnyAsync(e => e.Id == world.EventId));
        Assert.False(await db.WishlistItems.AnyAsync(w => w.Id == world.WishId));
    }

    [Fact]
    public async Task Deleting_An_Account_Revokes_Its_Refresh_Tokens()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var userId = await UserIdAsync(factory, "ana");
        await AddRefreshTokenAsync(factory, userId);

        var client = ClientWith(factory, new RecordingFileStorage(), world.AnaToken);
        (await client.DeleteAsync($"/api/user/{userId}")).EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var remaining = await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .RefreshTokens.CountAsync(r => r.UserId == userId);

        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task The_Other_Couple_Is_Left_Alone()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var spy = new RecordingFileStorage();
        var client = ClientWith(factory, spy, world.AnaToken);
        var userId = await UserIdAsync(factory, "ana");

        (await client.DeleteAsync($"/api/user/{userId}")).EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(1, await db.Couples.CountAsync());
        Assert.True(await db.Users.AnyAsync(u => u.Username == "lejla"));
    }

    [Fact]
    public async Task A_User_Cannot_Delete_Someone_Else()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var spy = new RecordingFileStorage();
        var client = ClientWith(factory, spy, world.LejlaToken);
        var anaId = await UserIdAsync(factory, "ana");

        var response = await client.DeleteAsync($"/api/user/{anaId}");

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(spy.Deleted);
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

    private static async Task<int> UserIdAsync(OurSpaceFactory factory, string username)
    {
        using var scope = factory.Services.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Users.Where(u => u.Username == username).Select(u => u.Id).SingleAsync();
    }

    private static async Task<string> EmailAsync(OurSpaceFactory factory, int userId)
    {
        using var scope = factory.Services.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Users.Where(u => u.Id == userId).Select(u => u.Email).SingleAsync();
    }

    private static async Task AddRefreshTokenAsync(OurSpaceFactory factory, int userId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = "live-session",
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(10),
        });

        await db.SaveChangesAsync();
    }

    private sealed class RecordingFileStorage : IFileStorageService
    {
        public List<string> Deleted { get; } = [];

        public Task DeleteAsync(string url)
        {
            Deleted.Add(url);
            return Task.CompletedTask;
        }

        public Task<StoredFile> SaveAsync(Stream content, string subfolder, string fileExtension) =>
            Task.FromResult(new StoredFile($"/uploads/{subfolder}/spy{fileExtension}", 0));

        public Task<Stream> OpenReadAsync(string url) => Task.FromResult<Stream>(new MemoryStream());
    }
}
