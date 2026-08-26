namespace OurSpace.API.Models.DTOs.Auth;

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Code, string NewPassword);
