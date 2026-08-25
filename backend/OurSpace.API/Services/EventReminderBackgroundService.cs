using Microsoft.EntityFrameworkCore;
using OurSpace.API.Data;

namespace OurSpace.API.Services;

/// <summary>
/// Periodically checks for upcoming events (within 3 days) and sends a one-time reminder
/// email to both partners via IEmailService. Runs every 6 hours.
/// </summary>
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
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = DateTime.UtcNow;
        var reminderCutoff = now.AddDays(ReminderWindowDays);

        var dueEvents = await db.Events
            .Include(e => e.Couple).ThenInclude(c => c.User1)
            .Include(e => e.Couple).ThenInclude(c => c.User2)
            .Where(e => e.ReminderSentAt == null && e.EventDate >= now && e.EventDate <= reminderCutoff)
            .ToListAsync(stoppingToken);

        foreach (var ev in dueEvents)
        {
            var subject = $"Podsjetnik: {ev.Title}";
            var body = $"Događaj \"{ev.Title}\" je zakazan za {ev.EventDate:dd.MM.yyyy.}.";

            await emailService.SendAsync(ev.Couple.User1.Email, subject, body);
            await emailService.SendAsync(ev.Couple.User2.Email, subject, body);

            ev.ReminderSentAt = now;
        }

        if (dueEvents.Count > 0)
            await db.SaveChangesAsync(stoppingToken);
    }
}
