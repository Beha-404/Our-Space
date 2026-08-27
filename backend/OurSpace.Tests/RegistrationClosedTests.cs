using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OurSpace.Tests;

public class RegistrationClosedTests
{
    [Fact]
    public async Task Registration_Is_Rejected_When_Closed()
    {
        using var factory = new OurSpaceFactory(registrationOpen: false);

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { username = "uljez", email = "uljez@test.local", password = "Lozinka123" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Registration_Works_When_Opened()
    {
        using var factory = new OurSpaceFactory(registrationOpen: true);

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { username = "novi", email = "novi@test.local", password = "Lozinka123" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
