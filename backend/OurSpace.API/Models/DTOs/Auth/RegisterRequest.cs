namespace OurSpace.API.Models.DTOs.Auth;

public record RegisterRequest(string Username, string Email, string Password, string? InviteCode);

public record RegisterResponse(bool NeedsApproval, AuthResponse? Auth);
