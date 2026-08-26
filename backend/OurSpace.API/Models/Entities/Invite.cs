namespace OurSpace.API.Models.Entities;

public class Invite
{
    public int Id { get; set; }

    public required string Code { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    public int? UsedByUserId { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public bool IsUsable => UsedAt is null && RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}
