using OurSpace.API.Models.DTOs.Auth;

namespace OurSpace.API.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<LoginOutcome> LoginAsync(LoginRequest request, string? deviceToken);
    Task<AuthResult> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
    Task<VerifyLoginResult> VerifyLoginAsync(VerifyLoginRequest request);

    Task RequestPasswordResetAsync(string email);
    Task ResetPasswordAsync(ResetPasswordRequest request);
}
