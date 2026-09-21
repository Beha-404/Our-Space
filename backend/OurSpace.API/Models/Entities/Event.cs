namespace OurSpace.API.Models.Entities;

public class Event
{
    public int Id { get; set; }

    public int CoupleId { get; set; }
    public Couple Couple { get; set; } = null!;

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReminderSentAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}
