using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Auth;
using OurSpace.API.Models.Entities;
using Xunit;

namespace OurSpace.Tests;

public class InviteRegistrationTests
{
    [Fact]
    public async Task First_Account_On_An_Empty_Database_Needs_No_Invite()
    {
        using var factory = new OurSpaceFactory();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { username = "prvi", email = "prvi@test.local", password = "Lozinka123" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(body);
        Assert.False(body.NeedsApproval);
        Assert.NotNull(body.Auth);
    }

    [Fact]
    public async Task Registration_Without_An_Invite_Is_Rejected_Once_Users_Exist()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { username = "uljez", email = "uljez@test.local", password = "Lozinka123" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_Invite_Code_Is_Rejected()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { username = "uljez", email = "uljez@test.local", password = "Lozinka123", inviteCode = "XXXXXXXX" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Valid_Invite_Creates_An_Account_That_Cannot_Log_In_Yet()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var code = await CreateInviteAsync(factory, world.AnaToken);

        var register = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { username = "gost", email = "gost@test.local", password = "Lozinka123", inviteCode = code });

        Assert.Equal(HttpStatusCode.Accepted, register.StatusCode);

        var body = await register.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(body);
        Assert.True(body.NeedsApproval);
        Assert.Null(body.Auth);

        var login = await factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { username = "gost", password = "Lozinka123" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Invite_Cannot_Be_Used_Twice()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var code = await CreateInviteAsync(factory, world.AnaToken);
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/register",
            new { username = "gost", email = "gost@test.local", password = "Lozinka123", inviteCode = code });

        var second = await client.PostAsJsonAsync("/api/auth/register",
            new { username = "gost2", email = "gost2@test.local", password = "Lozinka123", inviteCode = code });

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Approval_Unlocks_The_Account()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var code = await CreateInviteAsync(factory, world.AnaToken);

        await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { username = "gost", email = "gost@test.local", password = "Lozinka123", inviteCode = code });

        var approvalCode = await ReadApprovalCodeAsync(factory, "gost");

        var approve = await TestWorld.ClientFor(factory, world.AnaToken)
            .PostAsJsonAsync("/api/invites/approve", new { code = approvalCode });

        approve.EnsureSuccessStatusCode();

        var login = await factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { username = "gost", password = "Lozinka123" });

        login.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Someone_Elses_Invite_Cannot_Be_Approved_By_An_Unrelated_User()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var code = await CreateInviteAsync(factory, world.AnaToken);

        await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { username = "gost", email = "gost@test.local", password = "Lozinka123", inviteCode = code });

        var approvalCode = await ReadApprovalCodeAsync(factory, "gost");

        var approve = await TestWorld.ClientFor(factory, world.LejlaToken)
            .PostAsJsonAsync("/api/invites/approve", new { code = approvalCode });

        Assert.Equal(HttpStatusCode.BadRequest, approve.StatusCode);
    }

    [Fact]
    public async Task Revoked_Invite_Cannot_Be_Used()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.AnaToken);
        var created = await client.PostAsJsonAsync("/api/invites", new { });
        var invite = await created.Content.ReadFromJsonAsync<InviteDto>();

        var revoke = await client.DeleteAsync($"/api/invites/{invite!.Id}");
        revoke.EnsureSuccessStatusCode();

        var register = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { username = "gost", email = "gost@test.local", password = "Lozinka123", inviteCode = invite.Code });

        Assert.Equal(HttpStatusCode.BadRequest, register.StatusCode);
    }

    private static async Task<string> CreateInviteAsync(OurSpaceFactory factory, string token)
    {
        var response = await TestWorld.ClientFor(factory, token).PostAsJsonAsync("/api/invites", new { });
        response.EnsureSuccessStatusCode();

        var invite = await response.Content.ReadFromJsonAsync<InviteDto>();
        return invite!.Code;
    }

    private static async Task<string> ReadApprovalCodeAsync(OurSpaceFactory factory, string username)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.SingleAsync(u => u.Username == username);
        return user.ApprovalCode!;
    }
}
