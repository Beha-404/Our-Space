using Microsoft.EntityFrameworkCore;
using OurSpace.API.Data;

namespace OurSpace.API.Services;

public class EmailDispatchBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<EmailDispatchBackgroundService> logger) : BackgroundService
{
    private const string JobName = "email-dispatch";
    private const int BatchSize = 20;
    private const int MaxAttempts = 5;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Email dispatch failed");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task DispatchBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var leases = scope.ServiceProvider.GetRequiredService<IJobLeaseService>();

        if (!await leases.TryAcquireAsync(JobName, LeaseDuration, stoppingToken))
            return;

        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var now = DateTime.UtcNow;

        var pending = await db.OutboxEmails
            .Where(e => e.SentAt == null
                && e.Attempts < MaxAttempts
                && (e.NextAttemptAt == null || e.NextAttemptAt <= now))
            .OrderBy(e => e.Id)
            .Take(BatchSize)
            .ToListAsync(stoppingToken);

        if (pending.Count == 0)
            return;

        foreach (var email in pending)
        {
            try
            {
                await emailService.SendAsync(email.ToEmail, email.Subject, email.Body);
                email.SentAt = DateTime.UtcNow;
                email.LastError = null;
            }
            catch (Exception ex)
            {
                email.Attempts++;
                email.LastError = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                email.NextAttemptAt = DateTime.UtcNow.AddMinutes(Math.Pow(3, email.Attempts));

                logger.LogWarning(ex, "Email {Id} to {To} failed (attempt {Attempt})",
                    email.Id, email.ToEmail, email.Attempts);
            }
        }

        await db.SaveChangesAsync(stoppingToken);
    }
}
