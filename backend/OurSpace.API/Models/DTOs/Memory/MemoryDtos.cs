namespace OurSpace.API.Models.DTOs.Memory;

public class MemoryRow
{
    public int Id { get; set; }
    public required string Type { get; set; }
    public required string Url { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? MediumUrl { get; set; }
    public string? Caption { get; set; }
    public string Status { get; set; } = "ready";
    public DateOnly Date { get; set; }
    public required string UploadedByUsername { get; set; }
    public DateTime CreatedAt { get; set; }
}

public record MemoryFeedDto(
    List<MemoryRow> Items,
    bool HasMore,
    List<int>? Years
);
