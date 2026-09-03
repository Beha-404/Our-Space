using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OurSpace.Tests;

public class ForwardedClientIpTests
{
    [Fact]
    public async Task Two_Visitors_Behind_The_Proxy_Get_Separate_Rate_Limit_Buckets()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var visitorA = ClientFrom(factory, "203.0.113.10");
        var visitorB = ClientFrom(factory, "203.0.113.20");

        for (var i = 0; i < 10; i++)
            await Login(visitorA, "nema-me", "PogresnaSifra1");

        var visitorAThrottled = await Login(visitorA, "nema-me", "PogresnaSifra1");
        var visitorBStillAllowed = await Login(visitorB, "nema-me", "PogresnaSifra1");

        Assert.Equal(HttpStatusCode.TooManyRequests, visitorAThrottled.StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, visitorBStillAllowed.StatusCode);
    }

    [Fact]
    public async Task A_Spoofed_Forwarded_Chain_Still_Resolves_To_The_Real_Client()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "198.51.100.7, 10.0.0.4");

        var response = await Login(client, "nema-me", "PogresnaSifra1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Requests_With_No_Forwarded_Header_Still_Work()
    {
        using var factory = new OurSpaceFactory();
        await TestWorld.SeedAsync(factory);

        var response = await Login(factory.CreateClient(), "nema-me", "PogresnaSifra1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static HttpClient ClientFrom(OurSpaceFactory factory, string ip)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", ip);

        return client;
    }

    private static Task<HttpResponseMessage> Login(HttpClient client, string username, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { username, password });
}
