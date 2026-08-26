namespace OurSpace.API.Models.Entities;

public class WishlistItem
{
    public int Id { get; set; }

    public int CoupleId { get; set; }
    public Couple Couple { get; set; } = null!;

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public required string Title { get; set; }

    public bool IsFulfilled { get; set; }
    public DateTime? FulfilledAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
