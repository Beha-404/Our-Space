using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.User;
using OurSpace.API.Models.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace OurSpace.API.Services;

public partial class UserService(
    AppDbContext db,
    IFileStorageService fileStorage,
    ILocalizer localizer,
    IEmailQueue emailQueue,
    IFileUrlSigner urlSigner) : IUserService
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024;
    private const int AvatarMaxSize = 400;
    private const int MaxUsernameLength = 30;
    private const int MaxEmailLength = 254;

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

            if (username.Length > MaxUsernameLength)
                throw new BadRequestException(localizer.T("Auth.UsernameTooLong"));

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

        if (newEmail.Length > MaxEmailLength)
            throw new BadRequestException(localizer.T("Auth.EmailTooLong"));

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
        await emailQueue.EnqueueAsync(user.Email, subject, body);
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

        _ = await ImageFormats.ResolveExtensionAsync(file)
            ?? throw new BadRequestException(localizer.T("User.PictureUnsupportedFormat"));

        var user = await GetUserOrThrow(userId);
        var oldPictureUrl = user.ProfilePictureUrl;

        StoredFile stored;

        await using (var uploadStream = file.OpenReadStream())
        using (var image = await Image.LoadAsync(uploadStream))
        {
            image.Mutate(x => x.AutoOrient().Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(AvatarMaxSize, AvatarMaxSize),
            }));

            await using var buffer = new MemoryStream();
            await image.SaveAsWebpAsync(buffer);
            buffer.Position = 0;

            stored = await fileStorage.SaveAsync(buffer, "avatars", ".webp");
        }

        user.ProfilePictureUrl = stored.Path;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(oldPictureUrl) && oldPictureUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            await fileStorage.DeleteAsync(oldPictureUrl);

        return await ToDto(user);
    }

    public async Task DeleteAsync(int userId)
    {
        var user = await GetUserOrThrow(userId);
        var couple = await db.Couples.SingleOrDefaultAsync(c => c.User1Id == userId || c.User2Id == userId);

        var storedPaths = new List<string>();

        if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
            storedPaths.Add(user.ProfilePictureUrl);

        if (couple is not null)
        {
            var photos = await db.Photos
                .Where(p => p.CoupleId == couple.Id)
                .Select(p => new { p.FilePath, p.ThumbnailPath, p.MediumPath })
                .ToListAsync();

            var audioPaths = await db.AudioMessages
                .Where(a => a.CoupleId == couple.Id)
                .Select(a => a.FilePath)
                .ToListAsync();

            foreach (var photo in photos)
            {
                storedPaths.Add(photo.FilePath);
                storedPaths.Add(photo.ThumbnailPath);
                if (photo.MediumPath is not null) storedPaths.Add(photo.MediumPath);
            }

            storedPaths.AddRange(audioPaths);

            db.Couples.Remove(couple);
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync();

        foreach (var path in storedPaths)
            await fileStorage.DeleteAsync(path);
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
        var code = request.Code?.Trim();
        if (string.IsNullOrEmpty(code) || !PairingCodeRegex().IsMatch(code))
            throw new BadRequestException(localizer.T("User.InvalidPairingCode"));

        var user = await GetUserOrThrow(userId);

        var alreadyPaired = await db.Couples.AnyAsync(c => c.User1Id == userId || c.User2Id == userId);
        if (alreadyPaired)
            throw new ConflictException(localizer.T("User.AlreadyPaired"));

        var partner = await db.Users.FirstOrDefaultAsync(u => u.PairingCode == code);
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

        var photoPaths = await db.Photos
            .Where(p => p.CoupleId == couple.Id)
            .Select(p => new { p.FilePath, p.ThumbnailPath, p.MediumPath })
            .ToListAsync();

        var audioPaths = await db.AudioMessages
            .Where(a => a.CoupleId == couple.Id)
            .Select(a => a.FilePath)
            .ToListAsync();

        db.Couples.Remove(couple);
        await db.SaveChangesAsync();

        foreach (var photo in photoPaths)
        {
            await fileStorage.DeleteAsync(photo.FilePath);
            await fileStorage.DeleteAsync(photo.ThumbnailPath);
            if (photo.MediumPath is not null) await fileStorage.DeleteAsync(photo.MediumPath);
        }

        foreach (var path in audioPaths)
            await fileStorage.DeleteAsync(path);

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
        var partner = await db.Couples
            .Where(c => c.User1Id == user.Id || c.User2Id == user.Id)
            .Select(c => new
            {
                Partner = c.User1Id == user.Id ? c.User2 : c.User1,
                c.RelationshipStartDate,
            })
            .Select(x => new PartnerDto(
                x.Partner.Id,
                x.Partner.Username,
                x.Partner.ProfilePictureUrl,
                x.RelationshipStartDate))
            .SingleOrDefaultAsync();

        var partnerDto = partner is null
            ? null
            : partner with { ProfilePictureUrl = urlSigner.Sign(partner.ProfilePictureUrl) };

        return new UserDto(user.Id, user.Username, user.Email, urlSigner.Sign(user.ProfilePictureUrl), partnerDto, user.PendingEmail);
    }

    private static string GenerateCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\d{6}$")]
    private static partial Regex PairingCodeRegex();
}
