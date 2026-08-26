using OurSpace.API.Models.DTOs.Auth;

namespace OurSpace.API.Services;

public interface IAuthService
{
    Task<RegisterOutcome> RegisterAsync(RegisterRequest request);
    Task<LoginOutcome> LoginAsync(LoginRequest request);
    Task<AuthResult> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
    Task<AuthResult> VerifyLoginAsync(VerifyLoginRequest request);

    Task RequestPasswordResetAsync(string email);
    Task ResetPasswordAsync(ResetPasswordRequest request);
}
