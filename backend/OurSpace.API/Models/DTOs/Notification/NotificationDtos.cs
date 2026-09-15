namespace OurSpace.API.Models.DTOs.Notification;

public record NotificationDto(
    int Id,
    string Type,
    string EntityType,
    int EntityId,
    string EntityTitle,
    string ActorUsername,
    DateTime CreatedAt,
    bool IsRead
);
