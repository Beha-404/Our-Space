using OurSpace.API.Models.DTOs.Notification;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public interface INotificationService
{
    Task NotifyAsync(int recipientUserId, int actorUserId, NotificationType type, string entityType, int entityId, string entityTitle);
    Task<List<NotificationDto>> GetRecentAsync(int userId, int skip = 0, int take = 20);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkReadAsync(int userId, int notificationId);
    Task MarkAllReadAsync(int userId);
}
