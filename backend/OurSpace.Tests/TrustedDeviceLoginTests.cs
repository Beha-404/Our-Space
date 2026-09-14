using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Auth;
using Xunit;

namespace OurSpace.Tests;

public class TrustedDeviceLoginTests
{
    private const string Username = "ana";
    private const string Password = "Lozinka123";
    private const string Email = "ana@test.local";

    private const string OtherUsername = "bojan";
    private const string OtherPassword = "Lozinka456";

    [Fact]
    public async Task Remembering_The_Device_Skips_Two_Factor_On_The_Next_Login()
    {
        using var factory = new OurSpaceFactory();
        await SeedLoginUserAsync(factory);

        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/login", new { username = Username, password = Password });
        var code = await ReadLoginCodeAsync(factory, Username);
        await client.PostAsJsonAsync("/api/auth/verify-login", new { username = Username, code, rememberDevice = true });

        var second = await client.PostAsJsonAsync("/api/auth/login", new { username = Username, password = Password });
        var body = await second.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.False(body!.RequiresTwoFactor);
        Assert.NotNull(body.Auth);
    }

    [Fact]
    public async Task Not_Remembering_The_Device_Still_Requires_Two_Factor_Next_Time()
    {
        using var factory = new OurSpaceFactory();
        await SeedLoginUserAsync(factory);

        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/login", new { username = Username, password = Password });
        var code = await ReadLoginCodeAsync(factory, Username);
        await client.PostAsJsonAsync("/api/auth/verify-login", new { username = Username, code, rememberDevice = false });

        var second = await client.PostAsJsonAsync("/api/auth/login", new { username = Username, password = Password });
        var body = await second.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.True(body!.RequiresTwoFactor);
    }

    [Fact]
    public async Task A_Trusted_Device_Cookie_Does_Not_Skip_Two_Factor_For_A_Different_User()
    {
        using var factory = new OurSpaceFactory();
        await SeedLoginUserAsync(factory);
        await SeedUserAsync(factory, OtherUsername, "bojan@test.local", OtherPassword);

        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/login", new { username = Username, password = Password });
        var code = await ReadLoginCodeAsync(factory, Username);
        await client.PostAsJsonAsync("/api/auth/verify-login", new { username = Username, code, rememberDevice = true });

        var second = await client.PostAsJsonAsync("/api/auth/login", new { username = OtherUsername, password = OtherPassword });
        var body = await second.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.True(body!.RequiresTwoFactor);
    }

    [Fact]
    public async Task Resetting_The_Password_Revokes_The_Trusted_Device()
    {
        using var factory = new OurSpaceFactory();
        await SeedLoginUserAsync(factory);

        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/login", new { username = Username, password = Password });
        var code = await ReadLoginCodeAsync(factory, Username);
        await client.PostAsJsonAsync("/api/auth/verify-login", new { username = Username, code, rememberDevice = true });

        await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = Email });
        var resetCode = await ReadPasswordResetCodeAsync(factory);
        const string newPassword = "NewPassword789";
        await client.PostAsJsonAsync("/api/auth/reset-password", new { email = Email, code = resetCode, newPassword });

        var second = await client.PostAsJsonAsync("/api/auth/login", new { username = Username, password = newPassword });
        var body = await second.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.True(body!.RequiresTwoFactor);
    }

    private static async Task SeedLoginUserAsync(OurSpaceFactory factory) =>
        await SeedUserAsync(factory, Username, Email, Password);

    private static async Task SeedUserAsync(OurSpaceFactory factory, string username, string email, string password)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Users.Add(new OurSpace.API.Models.Entities.User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(password),
        });

        await db.SaveChangesAsync();
    }

    private static async Task<string> ReadLoginCodeAsync(OurSpaceFactory factory, string username)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.SingleAsync(u => u.Username == username);
        return user.LoginCode!;
    }

    private static async Task<string> ReadPasswordResetCodeAsync(OurSpaceFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.SingleAsync(u => u.Username == Username);
        return user.PasswordResetCode!;
    }
}
