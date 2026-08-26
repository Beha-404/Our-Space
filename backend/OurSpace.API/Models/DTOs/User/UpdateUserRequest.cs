namespace OurSpace.API.Models.DTOs.User;

public record UpdateUserRequest(string? Username);

public record RequestEmailChangeRequest(string NewEmail);

public record ConfirmEmailChangeRequest(string Code);
