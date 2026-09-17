namespace OurSpace.API.Models.Entities;

public class Photo
{
    public int Id { get; set; }

    public int CoupleId { get; set; }
    public Couple Couple { get; set; } = null!;

    public int UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;

    public required string FilePath { get; set; }
    public required string ThumbnailPath { get; set; }
    public string? MediumPath { get; set; }
    public long SizeBytes { get; set; }
    public string? Caption { get; set; }
    public DateOnly TakenAt { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? LocationName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
