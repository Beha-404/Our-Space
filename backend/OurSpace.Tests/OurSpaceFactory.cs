using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OurSpace.API.Data;

namespace OurSpace.Tests;

public class OurSpaceFactory(long? quotaBytes = null, bool registrationOpen = true) : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Filename=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        if (quotaBytes.HasValue)
            builder.UseSetting("Storage:QuotaBytesPerCouple", quotaBytes.Value.ToString());

        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=test;Database=test;");
        builder.UseSetting("Jwt:Key", "oursspace-test-signing-key-do-not-use-in-production-0123456789");
        builder.UseSetting("Jwt:Issuer", "OurSpace.API");
        builder.UseSetting("Jwt:Audience", "OurSpace.Client");
        builder.UseSetting("Jwt:AccessTokenMinutes", "15");
        builder.UseSetting("Jwt:RefreshTokenDays", "30");
        builder.UseSetting("Auth:TwoFactorEnabled", "true");
        builder.UseSetting("Auth:RegistrationOpen", registrationOpen ? "true" : "false");

        connection.Open();

        builder.ConfigureServices(services =>
        {
            var efDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions)
                            || d.ServiceType == typeof(AppDbContext)
                            || (d.ServiceType.IsGenericType
                                && d.ServiceType.GetGenericArguments().Contains(typeof(AppDbContext))))
                .ToList();

            foreach (var descriptor in efDescriptors)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));

            services.RemoveAll<IHostedService>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            connection.Dispose();
    }
}
