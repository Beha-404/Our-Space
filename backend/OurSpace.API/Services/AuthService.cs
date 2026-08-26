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

    private static readonly TimeSpan ApprovalCodeLifetime = TimeSpan.FromHours(72);
    public async Task<RegisterOutcome> RegisterAsync(RegisterRequest request)
    {
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

        var isFirstAccount = !await db.Users.IgnoreQueryFilters().AnyAsync();
        var invite = isFirstAccount ? null : await ResolveInviteOrThrow(request.InviteCode);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password),
            PreferredLanguage = localizer.CurrentLang,
            IsApproved = isFirstAccount,
        };

        if (!isFirstAccount)
        {
            user.ApprovalCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            user.ApprovalCodeExpiresAt = DateTime.UtcNow.Add(ApprovalCodeLifetime);
        }

        db.Users.Add(user);
        await db.SaveChangesAsync();

        if (isFirstAccount)
            return new RegisterOutcome(false, await IssueTokensAsync(user));

        invite!.UsedByUserId = user.Id;
        invite.UsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var inviter = await db.Users.SingleAsync(u => u.Id == invite.CreatedByUserId);
        var subject = localizer.For("Email.AccountApproval.Subject", inviter.PreferredLanguage);
        var body = localizer.For("Email.AccountApproval.Body", inviter.PreferredLanguage,
            user.Username, user.Email, user.ApprovalCode!, (int)ApprovalCodeLifetime.TotalHours);

        emailQueue.Enqueue(inviter.Email, subject, body);

        return new RegisterOutcome(true, null);
    }

    private async Task<Invite> ResolveInviteOrThrow(string? inviteCode)
    {
        if (string.IsNullOrWhiteSpace(inviteCode))
            throw new BadRequestException(localizer.T("Invite.Required"));

        var normalized = inviteCode.Trim().ToUpperInvariant();

        var invite = await db.Invites.SingleOrDefaultAsync(i => i.Code == normalized)
            ?? throw new BadRequestException(localizer.T("Invite.Invalid"));

        if (!invite.IsUsable)
            throw new BadRequestException(localizer.T("Invite.Invalid"));

        return invite;
    }

    public async Task<LoginOutcome> LoginAsync(LoginRequest request)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Username == request.Username);
        if (user is null || !BCrypt.Net.BCrypt.EnhancedVerify(request.Password, user.PasswordHash))
            throw new UnauthorizedAppException(localizer.T("Auth.LoginFailed"));

        if (!user.IsApproved)
            throw new UnauthorizedAppException(localizer.T("Auth.NotApproved"));

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

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{9,}$")]
    private static partial Regex PasswordRegex();
}
