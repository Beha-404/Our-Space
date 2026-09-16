namespace OurSpace.API.Models.Entities;

public class TimeCapsule
{
    public int Id { get; set; }

    public int CoupleId { get; set; }
    public Couple Couple { get; set; } = null!;

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public required string Title { get; set; }
    public required string Message { get; set; }

    public DateTime? OpenAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? UnlockNotifiedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsUnlocked => OpenAt is null
        ? OpenedAt is not null
        : OpenAt <= DateTime.UtcNow;
}
