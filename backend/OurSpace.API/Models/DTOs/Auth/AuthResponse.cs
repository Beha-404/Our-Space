namespace OurSpace.API.Models.DTOs.Auth;

public record AuthResponse(
    string Token,
    string RefreshToken,
    DateTime ExpiresAt,
    int UserId,
    string Username,
    string Email
);
