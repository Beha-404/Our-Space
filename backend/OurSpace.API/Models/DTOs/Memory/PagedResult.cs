namespace OurSpace.API.Models.DTOs.Memory;

public record PagedResult<T>(List<T> Items, bool HasMore);
