namespace OurSpace.API.Models.DTOs.Auth;

public record AuthResponse(
    string Token,
    DateTime ExpiresAt,
    int UserId,
    string Username,
    string Email
);

public record AuthResult(AuthResponse Response, string RefreshToken);

public record LoginOutcome(bool RequiresTwoFactor, AuthResult? Result);


public record LoginResponse(bool RequiresTwoFactor, AuthResponse? Auth);

public record VerifyLoginRequest(string Username, string Code, bool RememberDevice = false);

public record VerifyLoginResult(AuthResult Result, string? DeviceToken);
