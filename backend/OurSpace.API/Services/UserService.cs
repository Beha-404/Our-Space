using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.User;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public partial class UserService(
    AppDbContext db,
    IFileStorageService fileStorage,
    ILocalizer localizer,
    IEmailQueue emailQueue,
    IFileUrlSigner urlSigner) : IUserService
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

        if (request.Username is not null)
        {
            var username = request.Username.Trim();
            if (string.IsNullOrWhiteSpace(username))
                throw new BadRequestException(localizer.T("User.UsernameRequired"));

            if (!string.Equals(username, user.Username, StringComparison.Ordinal))
            {
                var taken = await db.Users.AnyAsync(u => u.Id != userId && u.Username == username);
                if (taken)
                    throw new ConflictException(localizer.T("Auth.UsernameTaken"));

                user.Username = username;
            }
        }

        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await ToDto(user);
    }

    public async Task RequestEmailChangeAsync(int userId, string newEmail)
    {
        var user = await GetUserOrThrow(userId);
        newEmail = newEmail.Trim();

        if (!EmailRegex().IsMatch(newEmail))
            throw new BadRequestException(localizer.T("Auth.InvalidEmail"));

        if (string.Equals(newEmail, user.Email, StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException(localizer.T("User.SameEmail"));

        var taken = await db.Users.AnyAsync(u => u.Id != userId && u.Email == newEmail);
        if (taken)
            throw new ConflictException(localizer.T("Auth.EmailTaken"));

        var code = GenerateCode();
        user.PendingEmail = newEmail;
        user.EmailChangeCode = code;
        user.EmailChangeCodeExpiresAt = DateTime.UtcNow.AddHours(24);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var subject = localizer.For("Email.EmailChange.Subject", user.PreferredLanguage);
        var body = localizer.For("Email.EmailChange.Body", user.PreferredLanguage, newEmail, code);
        emailQueue.Enqueue(user.Email, subject, body);
    }

    public async Task<UserDto> ConfirmEmailChangeAsync(int userId, string code)
    {
        var user = await GetUserOrThrow(userId);

        if (string.IsNullOrWhiteSpace(user.PendingEmail) || user.EmailChangeCode is null)
            throw new BadRequestException(localizer.T("User.NoPendingEmailChange"));

        if (user.EmailChangeCode != code.Trim() || user.EmailChangeCodeExpiresAt < DateTime.UtcNow)
            throw new BadRequestException(localizer.T("User.InvalidEmailChangeCode"));

        var taken = await db.Users.AnyAsync(u => u.Id != userId && u.Email == user.PendingEmail);
        if (taken)
        {
            ClearPendingEmailChange(user);
            await db.SaveChangesAsync();
            throw new ConflictException(localizer.T("Auth.EmailTaken"));
        }

        user.Email = user.PendingEmail;
        ClearPendingEmailChange(user);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await ToDto(user);
    }

    public async Task<UserDto> CancelEmailChangeAsync(int userId)
    {
        var user = await GetUserOrThrow(userId);
        ClearPendingEmailChange(user);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await ToDto(user);
    }

    private static void ClearPendingEmailChange(User user)
    {
        user.PendingEmail = null;
        user.EmailChangeCode = null;
        user.EmailChangeCodeExpiresAt = null;
    }

    public async Task<UserDto> UpdateProfilePictureAsync(int userId, IFormFile file)
    {
        if (file.Length == 0)
            throw new BadRequestException(localizer.T("User.PictureFileEmpty"));

        if (file.Length > MaxFileSizeBytes)
            throw new BadRequestException(localizer.T("User.PictureTooLarge"));

        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new BadRequestException(localizer.T("User.PictureUnsupportedFormat"));

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
            throw new ConflictException(localizer.T("User.AlreadyPaired"));

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
            throw new ConflictException(localizer.T("User.AlreadyPaired"));

        var partner = await db.Users.SingleOrDefaultAsync(u => u.PairingCode == request.Code);
        if (partner is null || partner.PairingCodeExpiresAt < DateTime.UtcNow)
            throw new BadRequestException(localizer.T("User.InvalidPairingCode"));

        if (partner.Id == user.Id)
            throw new BadRequestException(localizer.T("User.CannotPairSelf"));

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

    public async Task<UserDto> UnpairAsync(int userId)
    {
        var user = await GetUserOrThrow(userId);

        var couple = await db.Couples.SingleOrDefaultAsync(c => c.User1Id == userId || c.User2Id == userId)
            ?? throw new BadRequestException(localizer.T("User.NotPaired"));

        var photos = await db.Photos.Where(p => p.CoupleId == couple.Id).ToListAsync();
        foreach (var photo in photos)
        {
            fileStorage.Delete(photo.FilePath);
            fileStorage.Delete(photo.ThumbnailPath);
        }

        var audioMessages = await db.AudioMessages.Where(a => a.CoupleId == couple.Id).ToListAsync();
        foreach (var audio in audioMessages)
        {
            fileStorage.Delete(audio.FilePath);
        }

        db.Couples.Remove(couple);
        await db.SaveChangesAsync();

        return await ToDto(user);
    }

    public async Task<UserDto> SetRelationshipDateAsync(int userId, SetRelationshipDateRequest request)
    {
        var user = await GetUserOrThrow(userId);

        var couple = await db.Couples.SingleOrDefaultAsync(c => c.User1Id == userId || c.User2Id == userId)
            ?? throw new BadRequestException(localizer.T("User.NotPaired"));

        couple.RelationshipStartDate = request.RelationshipStartDate;
        await db.SaveChangesAsync();

        return await ToDto(user);
    }

    public async Task UpdateLanguageAsync(int userId, string language)
    {
        if (!Localizer.SupportedLangs.Contains(language))
            throw new BadRequestException(localizer.T("User.UnsupportedLanguage"));

        var user = await GetUserOrThrow(userId);
        user.PreferredLanguage = language;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private async Task<User> GetUserOrThrow(int userId) =>
        await db.Users.SingleOrDefaultAsync(u => u.Id == userId)
        ?? throw new NotFoundException(localizer.T("User.NotFound"));

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
            partnerDto = new PartnerDto(partner.Id, partner.Username, urlSigner.Sign(partner.ProfilePictureUrl), couple.RelationshipStartDate);
        }

        return new UserDto(user.Id, user.Username, user.Email, urlSigner.Sign(user.ProfilePictureUrl), partnerDto, user.PendingEmail);
    }

    private static string GenerateCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
