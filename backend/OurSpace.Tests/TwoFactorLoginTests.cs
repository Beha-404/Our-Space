using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Auth;
using Xunit;

namespace OurSpace.Tests;

public class TwoFactorLoginTests
{
    private const string Username = "ana";
    private const string Password = "Lozinka123";

    [Fact]
    public async Task Login_With_Correct_Password_Does_Not_Return_A_Token()
    {
        using var factory = new OurSpaceFactory();
        await SeedLoginUserAsync(factory);

        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new { username = Username, password = Password });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(body);
        Assert.True(body.RequiresTwoFactor);
        Assert.Null(body.Auth);
    }

    [Fact]
    public async Task Correct_Code_Completes_The_Login()
    {
        using var factory = new OurSpaceFactory();
        await SeedLoginUserAsync(factory);

        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/login", new { username = Username, password = Password });

        var code = await ReadLoginCodeAsync(factory);
        var response = await client.PostAsJsonAsync("/api/auth/verify-login", new { username = Username, code });

        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
    }

    [Fact]
    public async Task Wrong_Code_Is_Rejected()
    {
        using var factory = new OurSpaceFactory();
        await SeedLoginUserAsync(factory);

        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/login", new { username = Username, password = Password });

        var response = await client.PostAsJsonAsync("/api/auth/verify-login", new { username = Username, code = "000000" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Code_Burns_After_Too_Many_Wrong_Attempts()
    {
        using var factory = new OurSpaceFactory();
        await SeedLoginUserAsync(factory);

        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/login", new { username = Username, password = Password });

        var code = await ReadLoginCodeAsync(factory);

        for (var attempt = 0; attempt < 5; attempt++)
            await client.PostAsJsonAsync("/api/auth/verify-login", new { username = Username, code = "000000" });

        var response = await client.PostAsJsonAsync("/api/auth/verify-login", new { username = Username, code });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Code_Cannot_Be_Reused()
    {
        using var factory = new OurSpaceFactory();
        await SeedLoginUserAsync(factory);

        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/login", new { username = Username, password = Password });

        var code = await ReadLoginCodeAsync(factory);
        await client.PostAsJsonAsync("/api/auth/verify-login", new { username = Username, code });

        var second = await client.PostAsJsonAsync("/api/auth/verify-login", new { username = Username, code });

        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
    }

    [Fact]
    public async Task Verify_Without_Logging_In_First_Is_Rejected()
    {
        using var factory = new OurSpaceFactory();
        await SeedLoginUserAsync(factory);

        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/verify-login", new { username = Username, code = "123456" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task SeedLoginUserAsync(OurSpaceFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Users.Add(new OurSpace.API.Models.Entities.User
        {
            Username = Username,
            Email = "ana@test.local",
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(Password),
        });

        await db.SaveChangesAsync();
    }

    private static async Task<string> ReadLoginCodeAsync(OurSpaceFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.SingleAsync(u => u.Username == Username);

        return user.LoginCode!;
    }
}
