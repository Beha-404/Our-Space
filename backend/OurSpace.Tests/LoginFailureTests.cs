using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OurSpace.API.Data;
using OurSpace.API.Models.Entities;
using Xunit;

namespace OurSpace.Tests;

public class LoginFailureTests
{
    private const string Username = "ana";
    private const string Password = "TajnaSifra123";

    [Fact]
    public async Task Unknown_User_And_Wrong_Password_Give_The_Same_Answer()
    {
        using var factory = new OurSpaceFactory();
        await SeedAsync(factory);

        var client = factory.CreateClient();

        var unknown = await client.PostAsJsonAsync("/api/auth/login", new { username = "nema-me", password = Password });
        var wrongPassword = await client.PostAsJsonAsync("/api/auth/login", new { username = Username, password = "PogresnaSifra1" });

        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(await unknown.Content.ReadAsStringAsync(), await wrongPassword.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_Corrupt_Stored_Hash_Is_Rejected_Not_Crashed()
    {
        using var factory = new OurSpaceFactory();
        await SeedAsync(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.Username == Username);
            user.PasswordHash = "not-a-real-hash";
            await db.SaveChangesAsync();
        }

        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new { username = Username, password = Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task SeedAsync(OurSpaceFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Users.Add(new User
        {
            Username = Username,
            Email = "ana@test.local",
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(Password),
        });

        await db.SaveChangesAsync();
    }
}
