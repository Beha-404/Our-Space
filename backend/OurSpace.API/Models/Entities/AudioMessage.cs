namespace OurSpace.API.Models.Entities;

public class AudioMessage
{
    public int Id { get; set; }

    public int CoupleId { get; set; }
    public Couple Couple { get; set; } = null!;

    public int UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;

    public required string FilePath { get; set; }
    public long SizeBytes { get; set; }
    public string? Caption { get; set; }
    public DateOnly RecordedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
