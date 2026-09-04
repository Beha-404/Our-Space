using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OurSpace.API.Data;
using OurSpace.API.Services;
using Xunit;

namespace OurSpace.Tests;

public class EmailOutboxTests
{
    [Fact]
    public async Task Queued_Mail_Survives_As_A_Database_Row()
    {
        using var factory = new OurSpaceFactory();
        using var scope = factory.Services.CreateScope();

        var queue = scope.ServiceProvider.GetRequiredService<IEmailQueue>();
        await queue.EnqueueAsync("ana@test.local", "Naslov", "Sadržaj");

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.OutboxEmails.SingleAsync();

        Assert.Equal("ana@test.local", stored.ToEmail);
        Assert.Null(stored.SentAt);
        Assert.Equal(0, stored.Attempts);
    }

    [Fact]
    public async Task Only_One_Instance_Can_Hold_A_Job_Lease()
    {
        using var factory = new OurSpaceFactory();

        using var first = factory.Services.CreateScope();
        using var second = factory.Services.CreateScope();

        var firstLease = first.ServiceProvider.GetRequiredService<IJobLeaseService>();
        var secondLease = second.ServiceProvider.GetRequiredService<IJobLeaseService>();

        var firstAcquired = await firstLease.TryAcquireAsync("test-job", TimeSpan.FromMinutes(5), CancellationToken.None);
        var secondAcquired = await secondLease.TryAcquireAsync("test-job", TimeSpan.FromMinutes(5), CancellationToken.None);

        Assert.True(firstAcquired);
        Assert.False(secondAcquired);
    }

    [Fact]
    public async Task An_Expired_Lease_Can_Be_Taken_Over()
    {
        using var factory = new OurSpaceFactory();

        using var first = factory.Services.CreateScope();
        var firstLease = first.ServiceProvider.GetRequiredService<IJobLeaseService>();

        Assert.True(await firstLease.TryAcquireAsync("stale-job", TimeSpan.FromMilliseconds(1), CancellationToken.None));

        await Task.Delay(30);

        using var second = factory.Services.CreateScope();
        var secondLease = second.ServiceProvider.GetRequiredService<IJobLeaseService>();

        Assert.True(await secondLease.TryAcquireAsync("stale-job", TimeSpan.FromMinutes(5), CancellationToken.None));
    }
}
