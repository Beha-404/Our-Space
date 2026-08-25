namespace OurSpace.API.Models.DTOs.Event;

public record CreateEventRequest(string Title, string? Description, DateTime EventDate);

public record EventDto(
    int Id,
    string Title,
    string? Description,
    DateTime EventDate,
    string CreatedByUsername,
    DateTime CreatedAt
);
