namespace OurSpace.API.Models.DTOs.User;

public record PairingCodeResponse(string Code, DateTime ExpiresAt);

public record PairRequest(string Code, DateOnly? RelationshipStartDate);
