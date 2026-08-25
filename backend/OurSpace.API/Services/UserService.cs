using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.User;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public class UserService(AppDbContext db, IFileStorageService fileStorage) : IUserService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public async Task<UserDto> GetCurrentAsync(int userId)
    {
        var user = await GetUserOrThrow(userId);
        return await ToDto(user);
    }

    public async Task<UserDto> UpdateAsync(int userId, UpdateUserRequest request)
    {
        var user = await GetUserOrThrow(userId);

        if (request.DisplayName is not null)
            user.DisplayName = request.DisplayName;
        if (request.ProfilePictureUrl is not null)
            user.ProfilePictureUrl = request.ProfilePictureUrl;

        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await ToDto(user);
    }

    public async Task<UserDto> UpdateProfilePictureAsync(int userId, IFormFile file)
    {
        if (file.Length == 0)
            throw new BadRequestException("Fajl je prazan.");

        if (file.Length > MaxFileSizeBytes)
            throw new BadRequestException("Slika je prevelika (maksimalno 5MB).");

        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new BadRequestException("Nepodržan format slike. Dozvoljeno: JPEG, PNG, WEBP, GIF.");

        var user = await GetUserOrThrow(userId);
        var oldPictureUrl = user.ProfilePictureUrl;

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension)) extension = ".jpg";

        await using var uploadStream = file.OpenReadStream();
        var newUrl = await fileStorage.SaveAsync(uploadStream, "avatars", extension);

        user.ProfilePictureUrl = newUrl;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(oldPictureUrl) && oldPictureUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            fileStorage.Delete(oldPictureUrl);

        return await ToDto(user);
    }

    public async Task DeleteAsync(int userId)
    {
        var user = await GetUserOrThrow(userId);
        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<PairingCodeResponse> GeneratePairingCodeAsync(int userId)
    {
        var user = await GetUserOrThrow(userId);

        var alreadyPaired = await db.Couples.AnyAsync(c => c.User1Id == userId || c.User2Id == userId);
        if (alreadyPaired)
            throw new ConflictException("Već si uparen/a sa partnerom.");

        user.PairingCode = GenerateCode();
        user.PairingCodeExpiresAt = DateTime.UtcNow.AddHours(24);
        await db.SaveChangesAsync();

        return new PairingCodeResponse(user.PairingCode, user.PairingCodeExpiresAt.Value);
    }

    public async Task<UserDto> PairAsync(int userId, PairRequest request)
    {
        var user = await GetUserOrThrow(userId);

        var alreadyPaired = await db.Couples.AnyAsync(c => c.User1Id == userId || c.User2Id == userId);
        if (alreadyPaired)
            throw new ConflictException("Već si uparen/a sa partnerom.");

        var partner = await db.Users.SingleOrDefaultAsync(u => u.PairingCode == request.Code);
        if (partner is null || partner.PairingCodeExpiresAt < DateTime.UtcNow)
            throw new BadRequestException("Nevažeći ili istekao kod za uparivanje.");

        if (partner.Id == user.Id)
            throw new BadRequestException("Ne možeš se upariti sam sa sobom.");

        var couple = new Couple
        {
            User1Id = user.Id,
            User2Id = partner.Id,
            RelationshipStartDate = request.RelationshipStartDate,
        };

        partner.PairingCode = null;
        partner.PairingCodeExpiresAt = null;
        user.PairingCode = null;
        user.PairingCodeExpiresAt = null;

        db.Couples.Add(couple);
        await db.SaveChangesAsync();

        return await ToDto(user);
    }

    public async Task<UserDto> SetRelationshipDateAsync(int userId, SetRelationshipDateRequest request)
    {
        var user = await GetUserOrThrow(userId);

        var couple = await db.Couples.SingleOrDefaultAsync(c => c.User1Id == userId || c.User2Id == userId)
            ?? throw new BadRequestException("Nisi uparen/a sa partnerom.");

        couple.RelationshipStartDate = request.RelationshipStartDate;
        await db.SaveChangesAsync();

        return await ToDto(user);
    }

    private async Task<User> GetUserOrThrow(int userId) =>
        await db.Users.SingleOrDefaultAsync(u => u.Id == userId)
        ?? throw new NotFoundException("Korisnik nije pronađen.");

    private async Task<UserDto> ToDto(User user)
    {
        var couple = await db.Couples
            .Include(c => c.User1)
            .Include(c => c.User2)
            .SingleOrDefaultAsync(c => c.User1Id == user.Id || c.User2Id == user.Id);

        PartnerDto? partnerDto = null;
        if (couple is not null)
        {
            var partner = couple.User1Id == user.Id ? couple.User2 : couple.User1;
            partnerDto = new PartnerDto(partner.Id, partner.Username, partner.DisplayName, partner.ProfilePictureUrl, couple.RelationshipStartDate);
        }

        return new UserDto(user.Id, user.Username, user.Email, user.DisplayName, user.ProfilePictureUrl, partnerDto);
    }

    private static string GenerateCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
}
