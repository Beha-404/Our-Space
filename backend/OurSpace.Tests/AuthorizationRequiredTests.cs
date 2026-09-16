using System.Net;
using Xunit;

namespace OurSpace.Tests;

public class AuthorizationRequiredTests
{
    [Theory]
    [InlineData("/api/events")]
    [InlineData("/api/photos")]
    [InlineData("/api/audio")]
    [InlineData("/api/wishlist")]
    [InlineData("/api/export/memories")]
    [InlineData("/api/notifications")]
    [InlineData("/api/notifications/unread-count")]
    public async Task Endpoint_Rejects_Request_Without_Token(string route)
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var client = factory.CreateClient();
        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/events")]
    [InlineData("/api/photos")]
    [InlineData("/api/audio")]
    [InlineData("/api/wishlist")]
    [InlineData("/api/export/memories")]
    [InlineData("/api/notifications")]
    [InlineData("/api/notifications/unread-count")]
    public async Task Endpoint_Rejects_Forged_Token(string route)
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var client = TestWorld.ClientFor(factory, "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.not-a-real-signature");
        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
