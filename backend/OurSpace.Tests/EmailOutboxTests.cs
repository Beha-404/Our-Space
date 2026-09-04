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

        var serverOne = LeaseServiceFor(factory, "server-1");
        var serverTwo = LeaseServiceFor(factory, "server-2");

        var firstAcquired = await serverOne.TryAcquireAsync("test-job", TimeSpan.FromMinutes(5), CancellationToken.None);
        var secondAcquired = await serverTwo.TryAcquireAsync("test-job", TimeSpan.FromMinutes(5), CancellationToken.None);

        Assert.True(firstAcquired);
        Assert.False(secondAcquired);
    }

    private static IJobLeaseService LeaseServiceFor(OurSpaceFactory factory, string instanceId)
    {
        var db = factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
        return new JobLeaseService(db, new FixedInstanceIdentity(instanceId));
    }

    private sealed record FixedInstanceIdentity(string Value) : IInstanceIdentity;

    [Fact]
    public async Task The_Current_Holder_Can_Keep_Working_On_The_Next_Round()
    {
        using var factory = new OurSpaceFactory();
        using var scope = factory.Services.CreateScope();

        var lease = scope.ServiceProvider.GetRequiredService<IJobLeaseService>();

        Assert.True(await lease.TryAcquireAsync("repeat-job", TimeSpan.FromMinutes(15), CancellationToken.None));
        Assert.True(await lease.TryAcquireAsync("repeat-job", TimeSpan.FromMinutes(15), CancellationToken.None));
    }

    [Fact]
    public async Task An_Expired_Lease_Can_Be_Taken_Over()
    {
        using var factory = new OurSpaceFactory();

        var deadServer = LeaseServiceFor(factory, "server-that-died");
        Assert.True(await deadServer.TryAcquireAsync("stale-job", TimeSpan.FromMilliseconds(1), CancellationToken.None));

        await Task.Delay(30);

        var takingOver = LeaseServiceFor(factory, "server-taking-over");
        Assert.True(await takingOver.TryAcquireAsync("stale-job", TimeSpan.FromMinutes(5), CancellationToken.None));
    }
}
