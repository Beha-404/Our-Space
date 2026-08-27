using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OurSpace.API.Common;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;
using OurSpace.API.Models.DTOs.Auth;
using OurSpace.API.Options;
using OurSpace.API.Services;

namespace OurSpace.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IAuthService authService,
    IOptions<AuthCookieOptions> cookieOptions,
    IOptions<JwtOptions> jwtOptions,
    ILocalizer localizer) : ControllerBase
{
    private string CookieName => cookieOptions.Value.Secure ? "__Secure-osRefresh" : "osRefresh";

    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        SetRefreshCookie(result.RefreshToken);
        return Created(string.Empty, result.Response);
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var outcome = await authService.LoginAsync(request);

        if (outcome.RequiresTwoFactor)
            return Ok(new LoginResponse(true, null));

        SetRefreshCookie(outcome.Result!.RefreshToken);
        return Ok(new LoginResponse(false, outcome.Result.Response));
    }

    [HttpPost("verify-login")]
    [EnableRateLimiting(RateLimitPolicies.VerifyLogin)]
    public async Task<ActionResult<AuthResponse>> VerifyLogin(VerifyLoginRequest request)
    {
        var result = await authService.VerifyLoginAsync(request);
        SetRefreshCookie(result.RefreshToken);
        return Ok(result.Response);
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitPolicies.ForgotPassword)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        await authService.RequestPasswordResetAsync(request.Email);
        return NoContent();
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitPolicies.ResetPassword)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        await authService.ResetPasswordAsync(request);
        ClearRefreshCookie();
        return NoContent();
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh()
    {
        var refreshToken = Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedAppException(localizer.T("Auth.InvalidRefreshToken"));

        var result = await authService.RefreshAsync(refreshToken);
        SetRefreshCookie(result.RefreshToken);
        return Ok(result.Response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies[CookieName];
        if (!string.IsNullOrWhiteSpace(refreshToken))
            await authService.LogoutAsync(refreshToken);

        ClearRefreshCookie();
        return NoContent();
    }

    private void SetRefreshCookie(string refreshToken)
    {
        Response.Cookies.Append(CookieName, refreshToken, BuildCookieOptions(
            DateTimeOffset.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays)));
    }

    private void ClearRefreshCookie()
    {
        Response.Cookies.Append(CookieName, string.Empty, BuildCookieOptions(DateTimeOffset.UnixEpoch));
    }

    private CookieOptions BuildCookieOptions(DateTimeOffset expires)
    {
        var opts = cookieOptions.Value;

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = opts.Secure,
            SameSite = opts.SameSite,
            Expires = expires,
            IsEssential = true,
            Path = "/api/auth",
            Domain = opts.Domain,
        };
    }
}
