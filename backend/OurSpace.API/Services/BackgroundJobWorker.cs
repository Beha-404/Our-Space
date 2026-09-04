using Microsoft.EntityFrameworkCore;
using OurSpace.API.Data;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public class BackgroundJobWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundJobWorker> logger) : BackgroundService
{
    private const string JobName = "background-jobs";
    private const int MaxAttempts = 3;
    private const int BatchSize = 5;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan StuckJobTimeout = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background job batch failed");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task RunBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var leases = scope.ServiceProvider.GetRequiredService<IJobLeaseService>();

        if (!await leases.TryAcquireAsync(JobName, LeaseDuration, stoppingToken))
            return;

        var stuckBefore = DateTime.UtcNow - StuckJobTimeout;

        var pending = await db.BackgroundJobs
            .Where(j => j.Attempts < MaxAttempts
                && (j.Status == BackgroundJobStatus.Pending
                    || (j.Status == BackgroundJobStatus.Running && j.CreatedAt < stuckBefore)))
            .OrderBy(j => j.Id)
            .Take(BatchSize)
            .ToListAsync(stoppingToken);

        if (pending.Count == 0)
            return;

        var handlers = scope.ServiceProvider.GetServices<IBackgroundJobHandler>()
            .ToDictionary(h => h.JobType);

        foreach (var job in pending)
        {
            if (!await leases.TryAcquireAsync(JobName, LeaseDuration, stoppingToken))
                return;

            job.Status = BackgroundJobStatus.Running;
            job.Attempts++;
            await db.SaveChangesAsync(stoppingToken);

            try
            {
                if (!handlers.TryGetValue(job.Type, out var handler))
                    throw new InvalidOperationException($"No handler registered for job type '{job.Type}'.");

                await handler.HandleAsync(job, stoppingToken);

                job.Status = BackgroundJobStatus.Done;
                job.CompletedAt = DateTime.UtcNow;
                job.LastError = null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background job {Id} of type {Type} failed on attempt {Attempt}",
                    job.Id, job.Type, job.Attempts);

                job.LastError = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                job.Status = job.Attempts >= MaxAttempts
                    ? BackgroundJobStatus.Failed
                    : BackgroundJobStatus.Pending;
            }

            await db.SaveChangesAsync(stoppingToken);
        }
    }
}
