using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OurSpace.API.Data;
using OurSpace.API.Models.Entities;
using OurSpace.API.Services;
using Xunit;

namespace OurSpace.Tests;

public class PairingSecurityTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12a456")]
    public async Task Malformed_Code_Is_Rejected_Without_A_Server_Error(string code)
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, await UnpairedTokenAsync(factory, "sanja"));
        var response = await client.PostAsJsonAsync("/api/user/pair", new { code, relationshipStartDate = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Blank_Code_Does_Not_Match_Users_Who_Have_No_Pairing_Code()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var token = await UnpairedTokenAsync(factory, "sanja");
        var client = TestWorld.ClientFor(factory, token);

        var response = await client.PostAsJsonAsync("/api/user/pair", new { code = "", relationshipStartDate = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(2, await CoupleCountAsync(factory));
    }

    [Fact]
    public async Task Wrong_Code_Does_Not_Pair_Anyone()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, await UnpairedTokenAsync(factory, "sanja"));
        var response = await client.PostAsJsonAsync("/api/user/pair", new { code = "000000", relationshipStartDate = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(2, await CoupleCountAsync(factory));
    }

    [Fact]
    public async Task Guessing_Codes_Is_Rate_Limited()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, await UnpairedTokenAsync(factory, "sanja"));

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/user/pair", new { code = "123456", relationshipStartDate = (string?)null });
            statuses.Add(response.StatusCode);
        }

        Assert.Equal(5, statuses.Count(s => s == HttpStatusCode.BadRequest));
        Assert.Equal(3, statuses.Count(s => s == HttpStatusCode.TooManyRequests));
    }

    [Fact]
    public async Task A_Valid_Code_Still_Pairs_The_Couple()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var inviterToken = await UnpairedTokenAsync(factory, "sanja");
        var joinerToken = await UnpairedTokenAsync(factory, "tarik");

        var inviter = TestWorld.ClientFor(factory, inviterToken);
        var codeResponse = await inviter.PostAsync("/api/user/pairing-code", null);
        codeResponse.EnsureSuccessStatusCode();

        var code = (await codeResponse.Content.ReadFromJsonAsync<PairingCodeResponse>())!.Code;

        var joiner = TestWorld.ClientFor(factory, joinerToken);
        var response = await joiner.PostAsJsonAsync("/api/user/pair", new { code, relationshipStartDate = "2023-06-15" });

        response.EnsureSuccessStatusCode();
        Assert.Equal(3, await CoupleCountAsync(factory));
    }

    private static async Task<string> UnpairedTokenAsync(OurSpaceFactory factory, string name)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var user = new User
        {
            Username = name,
            Email = $"{name}@test.local",
            PasswordHash = "not-a-real-hash",
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return tokens.GenerateAccessToken(user);
    }

    private static async Task<int> CoupleCountAsync(OurSpaceFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Couples.CountAsync();
    }

    private record PairingCodeResponse(string Code, DateTime ExpiresAt);
}
