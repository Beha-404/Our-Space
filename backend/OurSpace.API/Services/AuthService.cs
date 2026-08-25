using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Auth;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public partial class AuthService(AppDbContext db, ITokenService tokenService) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (!EmailRegex().IsMatch(request.Email))
            throw new BadRequestException("Email adresa nije validna.");

        if (!PasswordRegex().IsMatch(request.Password))
            throw new BadRequestException("Lozinka mora imati najmanje 9 karaktera, jedno veliko slovo, jedno malo slovo i jedan broj.");

        var usernameTaken = await db.Users.AnyAsync(u => u.Username == request.Username);
        if (usernameTaken)
            throw new ConflictException("Korisničko ime je već zauzeto.");

        var emailTaken = await db.Users.AnyAsync(u => u.Email == request.Email);
        if (emailTaken)
            throw new ConflictException("Email je već registrovan.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password),
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Username == request.Username);
        if (user is null || !BCrypt.Net.BCrypt.EnhancedVerify(request.Password, user.PasswordHash))
            throw new UnauthorizedAppException("Pogrešno korisničko ime ili lozinka.");

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken)
    {
        var existing = await db.RefreshTokens
            .Include(r => r.User)
            .SingleOrDefaultAsync(r => r.Token == refreshToken);

        if (existing is null || !existing.IsActive)
            throw new UnauthorizedAppException("Nevažeći ili istekao refresh token.");

        existing.RevokedAt = DateTime.UtcNow;

        var response = await IssueTokensAsync(existing.User);
        existing.ReplacedByToken = response.RefreshToken;
        await db.SaveChangesAsync();

        return response;
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var existing = await db.RefreshTokens.SingleOrDefaultAsync(r => r.Token == refreshToken);
        if (existing is null || !existing.IsActive)
            return;

        existing.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private async Task<AuthResponse> IssueTokensAsync(User user)
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

        return new AuthResponse(
            Token: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: tokenService.AccessTokenExpiresAt(),
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email
        );
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{9,}$")]
    private static partial Regex PasswordRegex();
}
