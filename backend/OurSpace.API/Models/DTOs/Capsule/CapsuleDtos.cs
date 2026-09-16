namespace OurSpace.API.Models.DTOs.Capsule;

public record CreateCapsuleRequest(string Title, string Message, DateTime? OpenAt);

public record CapsuleDto(
    int Id,
    string Title,
    string? Message,
    DateTime? OpenAt,
    DateTime? OpenedAt,
    bool IsUnlocked,
    bool CanOpenNow,
    string CreatedByUsername,
    DateTime CreatedAt
);
