using Microsoft.EntityFrameworkCore;
using OurSpace.API.Data;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public interface IJobLeaseService
{
    Task<bool> TryAcquireAsync(string jobName, TimeSpan duration, CancellationToken cancellationToken);
}

public class JobLeaseService(AppDbContext db) : IJobLeaseService
{
    private static readonly string InstanceId =
        $"{Environment.MachineName}-{Environment.ProcessId}";

    public async Task<bool> TryAcquireAsync(string jobName, TimeSpan duration, CancellationToken cancellationToken)
    {
        await EnsureLeaseRowAsync(jobName, cancellationToken);

        var now = DateTime.UtcNow;
        var expiresAt = now.Add(duration);

        var claimed = await db.JobLeases
            .Where(l => l.Name == jobName && (l.ExpiresAt == null || l.ExpiresAt < now))
            .ExecuteUpdateAsync(s => s
                .SetProperty(l => l.Owner, InstanceId)
                .SetProperty(l => l.ExpiresAt, expiresAt), cancellationToken);

        return claimed == 1;
    }

    private async Task EnsureLeaseRowAsync(string jobName, CancellationToken cancellationToken)
    {
        var exists = await db.JobLeases.AnyAsync(l => l.Name == jobName, cancellationToken);
        if (exists)
            return;

        try
        {
            db.JobLeases.Add(new JobLease { Name = jobName });
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
        }
    }
}
