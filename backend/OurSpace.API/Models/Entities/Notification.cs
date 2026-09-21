namespace OurSpace.API.Models.Entities;

public enum NotificationType
{
    EventCreated,
    EventUpdated,
    EventDeleted,
    PhotoAdded,
    AudioAdded,
    WishAdded,
    CapsuleSealed,
    CapsuleUnlocked,
    EventCancelled,
    EventRestored,
}

public class Notification
{
    public int Id { get; set; }

    public int RecipientUserId { get; set; }
    public User RecipientUser { get; set; } = null!;

    public int ActorUserId { get; set; }
    public User ActorUser { get; set; } = null!;

    public required NotificationType Type { get; set; }
    public required string EntityType { get; set; }
    public int EntityId { get; set; }
    public required string EntityTitle { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
