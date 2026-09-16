using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public class CapsuleUnlockBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<CapsuleUnlockBackgroundService> logger) : BackgroundService
{
    private const string JobName = "capsule-unlocks";

    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AnnounceUnlockedCapsulesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Capsule unlock check failed");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task AnnounceUnlockedCapsulesAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var leases = scope.ServiceProvider.GetRequiredService<IJobLeaseService>();

        if (!await leases.TryAcquireAsync(JobName, LeaseDuration, stoppingToken))
            return;

        var emailQueue = scope.ServiceProvider.GetRequiredService<IEmailQueue>();
        var localizer = scope.ServiceProvider.GetRequiredService<ILocalizer>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;

        var due = await db.TimeCapsules
            .Where(c => c.UnlockNotifiedAt == null && c.OpenAt != null && c.OpenAt <= now)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.CreatedAt,
                c.CreatedByUserId,
                PartnerOneId = c.Couple.User1Id,
                PartnerOneEmail = c.Couple.User1.Email,
                PartnerOneLanguage = c.Couple.User1.PreferredLanguage,
                PartnerTwoId = c.Couple.User2Id,
                PartnerTwoEmail = c.Couple.User2.Email,
                PartnerTwoLanguage = c.Couple.User2.PreferredLanguage,
            })
            .ToListAsync(stoppingToken);

        if (due.Count == 0)
            return;

        foreach (var capsule in due)
        {
            var sealedOn = capsule.CreatedAt.ToString("dd.MM.yyyy.");

            var recipients = new[]
            {
                (Id: capsule.PartnerOneId, Email: capsule.PartnerOneEmail, Language: capsule.PartnerOneLanguage),
                (Id: capsule.PartnerTwoId, Email: capsule.PartnerTwoEmail, Language: capsule.PartnerTwoLanguage),
            };

            foreach (var (id, email, language) in recipients)
            {
                var subject = localizer.For("Email.CapsuleUnlocked.Subject", language, capsule.Title);
                var body = localizer.For("Email.CapsuleUnlocked.Body", language, sealedOn);
                await emailQueue.EnqueueAsync(email, subject, body);

                await notifications.NotifyAsync(
                    id, capsule.CreatedByUserId, NotificationType.CapsuleUnlocked, "capsule", capsule.Id, capsule.Title);
            }
        }

        var ids = due.Select(c => c.Id).ToList();

        await db.TimeCapsules
            .Where(c => ids.Contains(c.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UnlockNotifiedAt, now), stoppingToken);
    }
}
