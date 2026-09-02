using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Auth;
using OurSpace.API.Models.Entities;
using OurSpace.API.Options;

namespace OurSpace.API.Services;

public partial class AuthService(
    AppDbContext db,
    ITokenService tokenService,
    ILocalizer localizer,
    IEmailQueue emailQueue,
    IOptions<AuthOptions> authOptions) : IAuthService
{
    private static readonly TimeSpan ResetCodeLifetime = TimeSpan.FromMinutes(15);
    private const int MaxResetAttempts = 5;

    private static readonly TimeSpan LoginCodeLifetime = TimeSpan.FromMinutes(10);
    private const int MaxLoginCodeAttempts = 5;

    private static readonly TimeSpan RevokedTokenRetention = TimeSpan.FromDays(7);

    private static readonly Lazy<string> TimingDecoyHash =
        new(() => BCrypt.Net.BCrypt.EnhancedHashPassword("timing-decoy"));

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        if (!authOptions.Value.RegistrationOpen)
            throw new BadRequestException(localizer.T("Auth.RegistrationClosed"));

        if (!EmailRegex().IsMatch(request.Email))
            throw new BadRequestException(localizer.T("Auth.InvalidEmail"));

        if (!PasswordRegex().IsMatch(request.Password))
            throw new BadRequestException(localizer.T("Auth.WeakPassword"));

        var usernameTaken = await db.Users.AnyAsync(u => u.Username == request.Username);
        if (usernameTaken)
            throw new ConflictException(localizer.T("Auth.UsernameTaken"));

        var emailTaken = await db.Users.AnyAsync(u => u.Email == request.Email);
        if (emailTaken)
            throw new ConflictException(localizer.T("Auth.EmailTaken"));

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password),
            PreferredLanguage = localizer.CurrentLang,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return await IssueTokensAsync(user);
    }

    public async Task<LoginOutcome> LoginAsync(LoginRequest request)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Username == request.Username);

        var passwordMatches = VerifyPassword(request.Password, user?.PasswordHash);

        if (user is null || !passwordMatches)
            throw new UnauthorizedAppException(localizer.T("Auth.LoginFailed"));

        if (!authOptions.Value.TwoFactorEnabled)
            return new LoginOutcome(false, await IssueTokensAsync(user));

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        user.LoginCode = code;
        user.LoginCodeExpiresAt = DateTime.UtcNow.Add(LoginCodeLifetime);
        user.LoginCodeAttempts = 0;
        await db.SaveChangesAsync();

        var subject = localizer.For("Email.LoginCode.Subject", user.PreferredLanguage);
        var body = localizer.For("Email.LoginCode.Body", user.PreferredLanguage, code, (int)LoginCodeLifetime.TotalMinutes);
        emailQueue.Enqueue(user.Email, subject, body);

        return new LoginOutcome(true, null);
    }

    public async Task<AuthResult> VerifyLoginAsync(VerifyLoginRequest request)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Username == request.Username);

        if (user?.LoginCode is null || user.LoginCodeExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAppException(localizer.T("Auth.InvalidLoginCode"));

        if (user.LoginCodeAttempts >= MaxLoginCodeAttempts)
        {
            ClearLoginCode(user);
            await db.SaveChangesAsync();
            throw new UnauthorizedAppException(localizer.T("Auth.InvalidLoginCode"));
        }

        if (user.LoginCode != request.Code.Trim())
        {
            user.LoginCodeAttempts++;
            await db.SaveChangesAsync();
            throw new UnauthorizedAppException(localizer.T("Auth.InvalidLoginCode"));
        }

        ClearLoginCode(user);
        await db.SaveChangesAsync();

        return await IssueTokensAsync(user);
    }

    private static bool VerifyPassword(string password, string? storedHash)
    {
        var userExists = !string.IsNullOrWhiteSpace(storedHash);

        try
        {
            var matches = BCrypt.Net.BCrypt.EnhancedVerify(password, userExists ? storedHash : TimingDecoyHash.Value);
            return userExists && matches;
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    private static void ClearLoginCode(User user)
    {
        user.LoginCode = null;
        user.LoginCodeExpiresAt = null;
        user.LoginCodeAttempts = 0;
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken)
    {
        var existing = await db.RefreshTokens
            .Include(r => r.User)
            .SingleOrDefaultAsync(r => r.Token == refreshToken);

        if (existing is null || !existing.IsActive)
            throw new UnauthorizedAppException(localizer.T("Auth.InvalidRefreshToken"));

        existing.RevokedAt = DateTime.UtcNow;

        var result = await IssueTokensAsync(existing.User);
        existing.ReplacedByToken = result.RefreshToken;
        await db.SaveChangesAsync();

        return result;
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var existing = await db.RefreshTokens.SingleOrDefaultAsync(r => r.Token == refreshToken);
        if (existing is null || !existing.IsActive)
            return;

        existing.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task RequestPasswordResetAsync(string email)
    {
        var normalized = email.Trim();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == normalized);
        if (user is null)
            return;

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        user.PasswordResetCode = code;
        user.PasswordResetCodeExpiresAt = DateTime.UtcNow.Add(ResetCodeLifetime);
        user.PasswordResetAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var subject = localizer.For("Email.PasswordReset.Subject", user.PreferredLanguage);
        var body = localizer.For("Email.PasswordReset.Body", user.PreferredLanguage, code,
            (int)ResetCodeLifetime.TotalMinutes);
        emailQueue.Enqueue(user.Email, subject, body);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (!PasswordRegex().IsMatch(request.NewPassword))
            throw new BadRequestException(localizer.T("Auth.WeakPassword"));

        var normalized = request.Email.Trim();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == normalized);

        if (user?.PasswordResetCode is null || user.PasswordResetCodeExpiresAt < DateTime.UtcNow)
            throw new BadRequestException(localizer.T("Auth.InvalidResetCode"));

        if (user.PasswordResetAttempts >= MaxResetAttempts)
        {
            ClearResetCode(user);
            await db.SaveChangesAsync();
            throw new BadRequestException(localizer.T("Auth.InvalidResetCode"));
        }

        if (user.PasswordResetCode != request.Code.Trim())
        {
            user.PasswordResetAttempts++;
            await db.SaveChangesAsync();
            throw new BadRequestException(localizer.T("Auth.InvalidResetCode"));
        }

        user.PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(request.NewPassword);
        ClearResetCode(user);
        user.UpdatedAt = DateTime.UtcNow;

        var activeTokens = await db.RefreshTokens
            .Where(r => r.UserId == user.Id && r.RevokedAt == null)
            .ToListAsync();

        foreach (var token in activeTokens)
            token.RevokedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    private static void ClearResetCode(User user)
    {
        user.PasswordResetCode = null;
        user.PasswordResetCodeExpiresAt = null;
        user.PasswordResetAttempts = 0;
    }

    private async Task<AuthResult> IssueTokensAsync(User user)
    {
        await PruneRefreshTokensAsync(user.Id);

        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = tokenService.RefreshTokenExpiresAt(),
        });
        await db.SaveChangesAsync();

        var response = new AuthResponse(
            Token: accessToken,
            ExpiresAt: tokenService.AccessTokenExpiresAt(),
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email
        );

        return new AuthResult(response, refreshToken);
    }

    private async Task PruneRefreshTokensAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var revokedCutoff = now - RevokedTokenRetention;

        await db.RefreshTokens
            .IgnoreQueryFilters()
            .Where(r => r.UserId == userId
                && (r.ExpiresAt < now || (r.RevokedAt != null && r.RevokedAt < revokedCutoff)))
            .ExecuteDeleteAsync();
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{9,}$")]
    private static partial Regex PasswordRegex();
}
