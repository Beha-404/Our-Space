using System.Net;
using System.Net.Http.Json;
using OurSpace.API.Models.DTOs.Capsule;
using Xunit;

namespace OurSpace.Tests;

public class TimeCapsuleTests
{
    [Fact]
    public async Task A_Sealed_Capsule_Never_Sends_Its_Message_To_The_Client()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.MarkoToken);
        var capsules = await client.GetFromJsonAsync<List<CapsuleDto>>("/api/capsules");

        Assert.NotNull(capsules);

        var sealedCapsule = capsules.Single(c => c.Id == world.SealedCapsuleId);
        Assert.False(sealedCapsule.IsUnlocked);
        Assert.Null(sealedCapsule.Message);

        var raw = await client.GetStringAsync("/api/capsules");
        Assert.DoesNotContain(TestWorld.SealedCapsuleMessage, raw);
    }

    [Fact]
    public async Task Fetching_A_Sealed_Capsule_Directly_Is_Refused()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.MarkoToken);
        var response = await client.GetAsync($"/api/capsules/{world.SealedCapsuleId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain(TestWorld.SealedCapsuleMessage, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Opening_A_Sealed_Capsule_Early_Is_Refused()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.MarkoToken);
        var response = await client.PostAsync($"/api/capsules/{world.SealedCapsuleId}/open", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_Capsule_Past_Its_Date_Reveals_Its_Message()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, world.MarkoToken);
        var capsule = await client.GetFromJsonAsync<CapsuleDto>($"/api/capsules/{world.OpenCapsuleId}");

        Assert.NotNull(capsule);
        Assert.True(capsule.IsUnlocked);
        Assert.Equal(TestWorld.OpenCapsuleMessage, capsule.Message);
    }

    [Fact]
    public async Task The_Other_Couple_Cannot_See_Or_Open_A_Capsule()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var stranger = TestWorld.ClientFor(factory, world.LejlaToken);

        var list = await stranger.GetFromJsonAsync<List<CapsuleDto>>("/api/capsules");
        Assert.NotNull(list);
        Assert.Empty(list);

        var read = await stranger.GetAsync($"/api/capsules/{world.OpenCapsuleId}");
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);

        var deleted = await stranger.DeleteAsync($"/api/capsules/{world.SealedCapsuleId}");
        Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
    }

    [Fact]
    public async Task A_Partner_Cannot_Delete_A_Sealed_Capsule_They_Did_Not_Write()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var partner = TestWorld.ClientFor(factory, world.MarkoToken);
        var response = await partner.DeleteAsync($"/api/capsules/{world.SealedCapsuleId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var stillThere = await partner.GetFromJsonAsync<List<CapsuleDto>>("/api/capsules");
        Assert.NotNull(stillThere);
        Assert.Contains(stillThere, c => c.Id == world.SealedCapsuleId);
    }

    [Fact]
    public async Task The_Author_Can_Delete_Their_Own_Sealed_Capsule()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var author = TestWorld.ClientFor(factory, world.AnaToken);
        var response = await author.DeleteAsync($"/api/capsules/{world.SealedCapsuleId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task A_Partner_Cannot_Delete_An_Opened_Capsule_They_Did_Not_Write()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var partner = TestWorld.ClientFor(factory, world.MarkoToken);
        var response = await partner.DeleteAsync($"/api/capsules/{world.OpenCapsuleId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var stillThere = await partner.GetFromJsonAsync<List<CapsuleDto>>("/api/capsules");
        Assert.NotNull(stillThere);
        Assert.Contains(stillThere, c => c.Id == world.OpenCapsuleId);
    }

    [Fact]
    public async Task The_Author_Can_Delete_Their_Own_Opened_Capsule()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var author = TestWorld.ClientFor(factory, world.AnaToken);
        var response = await author.DeleteAsync($"/api/capsules/{world.OpenCapsuleId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Sealing_A_Capsule_Notifies_The_Partner_Without_Leaking_The_Message()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var author = TestWorld.ClientFor(factory, world.AnaToken);
        var request = new CreateCapsuleRequest("Otvori kad ti bude teško", "Tajna poruka za tebe", null);
        var created = await author.PostAsJsonAsync("/api/capsules", request);
        created.EnsureSuccessStatusCode();

        var partner = TestWorld.ClientFor(factory, world.MarkoToken);
        var raw = await partner.GetStringAsync("/api/notifications");

        Assert.Contains("CapsuleSealed", raw);
        Assert.Contains("Otvori kad ti bude teško", raw);
        Assert.DoesNotContain("Tajna poruka za tebe", raw);
    }

    [Fact]
    public async Task An_Undated_Capsule_Opens_Only_When_Someone_Chooses_To()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var author = TestWorld.ClientFor(factory, world.AnaToken);
        var created = await author.PostAsJsonAsync(
            "/api/capsules",
            new CreateCapsuleRequest("Otvori kad ti bude teško", "Tu sam.", null));

        var capsule = await created.Content.ReadFromJsonAsync<CapsuleDto>();
        Assert.NotNull(capsule);
        Assert.False(capsule.IsUnlocked);
        Assert.True(capsule.CanOpenNow);
        Assert.Null(capsule.Message);

        var partner = TestWorld.ClientFor(factory, world.MarkoToken);
        var opened = await partner.PostAsync($"/api/capsules/{capsule.Id}/open", null);
        opened.EnsureSuccessStatusCode();

        var revealed = await opened.Content.ReadFromJsonAsync<CapsuleDto>();
        Assert.NotNull(revealed);
        Assert.True(revealed.IsUnlocked);
        Assert.Equal("Tu sam.", revealed.Message);
    }

    [Fact]
    public async Task A_Capsule_Cannot_Be_Sealed_With_A_Date_In_The_Past()
    {
        using var factory = new OurSpaceFactory();
        var world = await TestWorld.SeedAsync(factory);

        var author = TestWorld.ClientFor(factory, world.AnaToken);
        var response = await author.PostAsJsonAsync(
            "/api/capsules",
            new CreateCapsuleRequest("Nazad u prošlost", "Ne bi trebalo proći", DateTime.UtcNow.AddDays(-2)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
