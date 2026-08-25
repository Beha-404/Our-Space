using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    DateTime AccessTokenExpiresAt();
    DateTime RefreshTokenExpiresAt();
}
