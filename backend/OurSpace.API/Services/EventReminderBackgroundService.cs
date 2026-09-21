using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;

namespace OurSpace.API.Services;

public class EventReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<EventReminderBackgroundService> logger) : BackgroundService
{
    private const string JobName = "event-reminders";
    private const int ReminderWindowDays = 3;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSendReminders(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Event reminder check failed");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckAndSendReminders(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var leases = scope.ServiceProvider.GetRequiredService<IJobLeaseService>();

        if (!await leases.TryAcquireAsync(JobName, LeaseDuration, stoppingToken))
            return;

        var emailQueue = scope.ServiceProvider.GetRequiredService<IEmailQueue>();
        var localizer = scope.ServiceProvider.GetRequiredService<ILocalizer>();

        var now = DateTime.UtcNow;
        var reminderCutoff = now.AddDays(ReminderWindowDays);

        var dueEvents = await db.Events
            .Where(e => e.ReminderSentAt == null && e.CancelledAt == null && e.EventDate >= now && e.EventDate <= reminderCutoff)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.EventDate,
                PartnerOneEmail = e.Couple.User1.Email,
                PartnerOneLanguage = e.Couple.User1.PreferredLanguage,
                PartnerTwoEmail = e.Couple.User2.Email,
                PartnerTwoLanguage = e.Couple.User2.PreferredLanguage,
            })
            .ToListAsync(stoppingToken);

        if (dueEvents.Count == 0)
            return;

        foreach (var ev in dueEvents)
        {
            var eventDateText = ev.EventDate.ToString("dd.MM.yyyy.");

            var recipients = new[]
            {
                (Email: ev.PartnerOneEmail, Language: ev.PartnerOneLanguage),
                (Email: ev.PartnerTwoEmail, Language: ev.PartnerTwoLanguage),
            };

            foreach (var (email, language) in recipients)
            {
                var subject = localizer.For("Email.Reminder.Subject", language, ev.Title);
                var body = localizer.For("Email.Reminder.Body", language, eventDateText);
                await emailQueue.EnqueueAsync(email, subject, body);
            }
        }

        var sentIds = dueEvents.Select(e => e.Id).ToList();

        await db.Events
            .Where(e => sentIds.Contains(e.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.ReminderSentAt, now), stoppingToken);
    }
}
