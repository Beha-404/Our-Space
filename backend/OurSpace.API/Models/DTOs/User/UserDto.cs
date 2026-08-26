namespace OurSpace.API.Models.DTOs.User;

public record UserDto(
    int Id,
    string Username,
    string Email,
    string? ProfilePictureUrl,
    PartnerDto? Partner,
    string? PendingEmail
);

public record PartnerDto(
    int Id,
    string Username,
    string? ProfilePictureUrl,
    DateOnly? RelationshipStartDate
);
