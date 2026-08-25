namespace OurSpace.API.Models.DTOs.User;

public record UserDto(
    int Id,
    string Username,
    string Email,
    string? DisplayName,
    string? ProfilePictureUrl,
    PartnerDto? Partner
);

public record PartnerDto(
    int Id,
    string Username,
    string? DisplayName,
    string? ProfilePictureUrl,
    DateOnly? RelationshipStartDate
);
