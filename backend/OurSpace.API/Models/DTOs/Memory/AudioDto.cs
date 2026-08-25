namespace OurSpace.API.Models.DTOs.Memory;

public record AudioDto(
    int Id,
    string Url,
    string? Caption,
    DateOnly RecordedAt,
    string UploadedByUsername,
    DateTime CreatedAt
);
