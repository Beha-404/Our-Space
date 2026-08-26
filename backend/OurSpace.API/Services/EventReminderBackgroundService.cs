using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;

namespace OurSpace.API.Services;

public class EventReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<EventReminderBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);
    private const int ReminderWindowDays = 3;

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
        var emailQueue = scope.ServiceProvider.GetRequiredService<IEmailQueue>();
        var localizer = scope.ServiceProvider.GetRequiredService<ILocalizer>();

        var now = DateTime.UtcNow;
        var reminderCutoff = now.AddDays(ReminderWindowDays);

        var dueEvents = await db.Events
            .Include(e => e.Couple).ThenInclude(c => c.User1)
            .Include(e => e.Couple).ThenInclude(c => c.User2)
            .Where(e => e.ReminderSentAt == null && e.EventDate >= now && e.EventDate <= reminderCutoff)
            .ToListAsync(stoppingToken);

        foreach (var ev in dueEvents)
        {
            var eventDateText = ev.EventDate.ToString("dd.MM.yyyy.");

            foreach (var recipient in new[] { ev.Couple.User1, ev.Couple.User2 })
            {
                var subject = localizer.For("Email.Reminder.Subject", recipient.PreferredLanguage, ev.Title);
                var body = localizer.For("Email.Reminder.Body", recipient.PreferredLanguage, ev.Title, eventDateText);
                emailQueue.Enqueue(recipient.Email, subject, body);
            }

            ev.ReminderSentAt = now;
        }

        if (dueEvents.Count > 0)
            await db.SaveChangesAsync(stoppingToken);
    }
}
