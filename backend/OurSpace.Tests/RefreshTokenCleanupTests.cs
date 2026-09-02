using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Auth;
using OurSpace.API.Models.Entities;
using OurSpace.API.Services;
using Xunit;

namespace OurSpace.Tests;

public class RefreshTokenCleanupTests
{
    private const string Username = "ana";
    private const string Password = "TajnaSifra123";

    [Fact]
    public async Task Issuing_A_Token_Clears_Out_Expired_Ones()
    {
        using var factory = new OurSpaceFactory(twoFactorEnabled: false);
        var userId = await SeedAsync(factory);

        await AddTokenAsync(factory, userId, "expired", expiresAt: DateTime.UtcNow.AddDays(-1));
        await AddTokenAsync(factory, userId, "still-valid", expiresAt: DateTime.UtcNow.AddDays(10));

        await IssueAsync(factory, userId);

        var remaining = await TokensAsync(factory, userId);

        Assert.DoesNotContain("expired", remaining);
        Assert.Contains("still-valid", remaining);
    }

    [Fact]
    public async Task Issuing_A_Token_Clears_Out_Long_Revoked_Ones()
    {
        using var factory = new OurSpaceFactory(twoFactorEnabled: false);
        var userId = await SeedAsync(factory);

        await AddTokenAsync(factory, userId, "revoked-long-ago",
            expiresAt: DateTime.UtcNow.AddDays(10), revokedAt: DateTime.UtcNow.AddDays(-30));

        await AddTokenAsync(factory, userId, "revoked-just-now",
            expiresAt: DateTime.UtcNow.AddDays(10), revokedAt: DateTime.UtcNow);

        await IssueAsync(factory, userId);

        var remaining = await TokensAsync(factory, userId);

        Assert.DoesNotContain("revoked-long-ago", remaining);
        Assert.Contains("revoked-just-now", remaining);
    }

    [Fact]
    public async Task Cleanup_Never_Touches_Another_Users_Tokens()
    {
        using var factory = new OurSpaceFactory(twoFactorEnabled: false);
        var userId = await SeedAsync(factory);
        var otherId = await SeedAsync(factory, "marko");

        await AddTokenAsync(factory, otherId, "someone-elses-expired", expiresAt: DateTime.UtcNow.AddDays(-1));

        await IssueAsync(factory, userId);

        Assert.Contains("someone-elses-expired", await TokensAsync(factory, otherId));
    }

    [Fact]
    public async Task Repeated_Refreshes_Do_Not_Pile_Up_Rows()
    {
        using var factory = new OurSpaceFactory(twoFactorEnabled: false);
        var userId = await SeedAsync(factory);

        for (var i = 0; i < 5; i++)
        {
            await AddTokenAsync(factory, userId, $"stale-{i}",
                expiresAt: DateTime.UtcNow.AddDays(10), revokedAt: DateTime.UtcNow.AddDays(-30));
        }

        await IssueAsync(factory, userId);

        Assert.Single(await TokensAsync(factory, userId));
    }

    private static async Task IssueAsync(OurSpaceFactory factory, int userId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var user = await db.Users.SingleAsync(u => u.Id == userId);

        await auth.LoginAsync(new LoginRequest(user.Username, Password));
    }

    private static async Task<List<string>> TokensAsync(OurSpaceFactory factory, int userId)
    {
        using var scope = factory.Services.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .RefreshTokens
            .IgnoreQueryFilters()
            .Where(r => r.UserId == userId)
            .Select(r => r.Token)
            .ToListAsync();
    }

    private static async Task AddTokenAsync(
        OurSpaceFactory factory, int userId, string token, DateTime expiresAt, DateTime? revokedAt = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = token,
            UserId = userId,
            ExpiresAt = expiresAt,
            RevokedAt = revokedAt,
        });

        await db.SaveChangesAsync();
    }

    private static async Task<int> SeedAsync(OurSpaceFactory factory, string username = Username)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Username = username,
            Email = $"{username}@test.local",
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(Password),
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user.Id;
    }
}
