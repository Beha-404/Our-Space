namespace OurSpace.API.Models.DTOs.Memory;

public record PhotoDto(
    int Id,
    string Url,
    string ThumbnailUrl,
    string? MediumUrl,
    string? Caption,
    DateOnly TakenAt,
    string UploadedByUsername,
    DateTime CreatedAt
);
