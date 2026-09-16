using Microsoft.EntityFrameworkCore;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Notification;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public class NotificationService(AppDbContext db) : INotificationService
{
    private const int MaxRecent = 30;
    private static readonly TimeSpan ReadNotificationRetention = TimeSpan.FromDays(30);

    public async Task NotifyAsync(int recipientUserId, int actorUserId, NotificationType type, string entityType, int entityId, string entityTitle)
    {
        db.Notifications.Add(new Notification
        {
            RecipientUserId = recipientUserId,
            ActorUserId = actorUserId,
            Type = type,
            EntityType = entityType,
            EntityId = entityId,
            EntityTitle = entityTitle,
        });

        await db.SaveChangesAsync();
        await PruneReadNotificationsAsync(recipientUserId);
    }

    private async Task PruneReadNotificationsAsync(int recipientUserId)
    {
        var cutoff = DateTime.UtcNow - ReadNotificationRetention;

        await db.Notifications
            .Where(n => n.RecipientUserId == recipientUserId && n.ReadAt != null && n.ReadAt < cutoff)
            .ExecuteDeleteAsync();
    }

    public async Task<List<NotificationDto>> GetRecentAsync(int userId) =>
        await db.Notifications
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(MaxRecent)
            .Select(n => new NotificationDto(
                n.Id, n.Type.ToString(), n.EntityType, n.EntityId, n.EntityTitle,
                n.ActorUser.Username, n.CreatedAt, n.ReadAt != null))
            .ToListAsync();

    public async Task<int> GetUnreadCountAsync(int userId) =>
        await db.Notifications.CountAsync(n => n.RecipientUserId == userId && n.ReadAt == null);

    public async Task MarkReadAsync(int userId, int notificationId)
    {
        var notification = await db.Notifications
            .SingleOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == userId);

        if (notification is null || notification.ReadAt is not null)
            return;

        notification.ReadAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task MarkAllReadAsync(int userId)
    {
        var now = DateTime.UtcNow;
        await db.Notifications
            .Where(n => n.RecipientUserId == userId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, now));
    }
}
