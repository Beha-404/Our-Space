using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Memory;
using OurSpace.API.Models.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace OurSpace.API.Services;

public class PhotoService(
    AppDbContext db,
    IFileStorageService fileStorage,
    ILocalizer localizer,
    IFileUrlSigner urlSigner,
    IStorageQuotaService quota,
    ICoupleContext coupleContext,
    INotificationService notifications) : IPhotoService
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024;
    private const int ThumbnailWidth = 640;
    private const int MediumWidth = 1600;
    private const int MaxPageSize = 500;
    private const int MaxCaptionLength = 300;

    public async Task<PhotoDto> UploadAsync(int userId, IFormFile file, DateOnly takenAt, string? caption)
    {
        ValidateFile(file);

        if (string.IsNullOrWhiteSpace(caption))
            throw new BadRequestException(localizer.T("Photo.TitleRequired"));

        if (caption.Trim().Length > MaxCaptionLength)
            throw new BadRequestException(localizer.T("Photo.CaptionTooLong"));

        var extension = await ImageFormats.ResolveExtensionAsync(file)
            ?? throw new BadRequestException(localizer.T("Photo.UnsupportedFormat"));

        var (coupleId, username) = await GetCoupleAndUsernameOrThrow(userId);

        await quota.EnsureRoomAsync(coupleId, file.Length);

        StoredFile original;

        await using (var uploadStream = file.OpenReadStream())
        {
            original = await fileStorage.SaveAsync(uploadStream, "photos", extension);
        }

        StoredFile? thumbnail = null;
        StoredFile? medium = null;

        try
        {
            (thumbnail, medium) = await GenerateVariantsAsync(file);

            var photo = new Photo
            {
                CoupleId = coupleId,
                UploadedByUserId = userId,
                FilePath = original.Path,
                ThumbnailPath = thumbnail.Path,
                MediumPath = medium.Path,
                SizeBytes = original.SizeBytes + thumbnail.SizeBytes + medium.SizeBytes,
                Caption = caption.Trim(),
                TakenAt = takenAt,
            };

            db.Photos.Add(photo);
            await db.SaveChangesAsync();

            var partnerId = await coupleContext.GetPartnerUserIdAsync(userId);
            if (partnerId is not null)
                await notifications.NotifyAsync(partnerId.Value, userId, NotificationType.PhotoAdded, "photo", photo.Id, photo.Caption);

            return ToDto(photo, username);
        }
        catch (Exception)
        {
            await fileStorage.DeleteAsync(original.Path);
            if (thumbnail is not null) await fileStorage.DeleteAsync(thumbnail.Path);
            if (medium is not null) await fileStorage.DeleteAsync(medium.Path);
            throw;
        }
    }

    public async Task<PagedResult<PhotoDto>> GetAllAsync(int userId, int page, int pageSize)
    {
        var coupleId = await coupleContext.GetCoupleIdOrThrow(userId, "Photo.NeedPartner");

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var photos = await db.Photos
            .Where(p => p.CoupleId == coupleId)
            .OrderByDescending(p => p.TakenAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(p => new PhotoDto(p.Id, p.FilePath, p.ThumbnailPath, p.MediumPath, p.Caption, p.TakenAt, p.UploadedByUser.Username, p.CreatedAt))
            .ToListAsync();

        var hasMore = photos.Count > pageSize;
        if (hasMore) photos.RemoveAt(photos.Count - 1);

        return new PagedResult<PhotoDto>(photos.Select(Signed).ToList(), hasMore);
    }

    public async Task<int> GetCountAsync(int userId)
    {
        var coupleId = await coupleContext.GetCoupleIdOrThrow(userId, "Photo.NeedPartner");
        return await db.Photos.CountAsync(p => p.CoupleId == coupleId);
    }

    public async Task DeleteAsync(int userId, int photoId)
    {
        var coupleId = await coupleContext.GetCoupleIdOrThrow(userId, "Photo.NeedPartner");

        var photo = await db.Photos.SingleOrDefaultAsync(p => p.Id == photoId)
            ?? throw new NotFoundException(localizer.T("Photo.NotFound"));

        if (photo.CoupleId != coupleId)
            throw new NotFoundException(localizer.T("Photo.NotFound"));

        db.Photos.Remove(photo);
        await db.SaveChangesAsync();
        await ClearQuotaWarningAsync(coupleId);

        await fileStorage.DeleteAsync(photo.FilePath);
        await fileStorage.DeleteAsync(photo.ThumbnailPath);
        if (photo.MediumPath is not null) await fileStorage.DeleteAsync(photo.MediumPath);
    }

    private void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
            throw new BadRequestException(localizer.T("Photo.FileEmpty"));

        if (file.Length > MaxFileSizeBytes)
            throw new BadRequestException(localizer.T("Photo.TooLarge"));
    }

    private async Task<(StoredFile Thumbnail, StoredFile Medium)> GenerateVariantsAsync(IFormFile file)
    {
        await using var uploadStream = file.OpenReadStream();
        using var image = await Image.LoadAsync(uploadStream);

        image.Mutate(x => x.AutoOrient());

        var medium = await SaveResizedAsync(image, MediumWidth, "medium");
        var thumbnail = await SaveResizedAsync(image, ThumbnailWidth, "thumbnails");

        return (thumbnail, medium);
    }

    private async Task<StoredFile> SaveResizedAsync(Image source, int maxSize, string subfolder)
    {
        using var resized = source.Clone(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(maxSize, maxSize),
        }));

        await using var buffer = new MemoryStream();
        await resized.SaveAsWebpAsync(buffer);
        buffer.Position = 0;

        return await fileStorage.SaveAsync(buffer, subfolder, ".webp");
    }

    private async Task ClearQuotaWarningAsync(int coupleId)
    {
        var couple = await db.Couples.SingleOrDefaultAsync(c => c.Id == coupleId);
        if (couple is null || couple.QuotaWarningEmailSentAt is null)
            return;

        couple.QuotaWarningEmailSentAt = null;
        await db.SaveChangesAsync();
    }

    private async Task<(int CoupleId, string Username)> GetCoupleAndUsernameOrThrow(int userId)
    {
        var row = await db.Couples
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .Select(c => new { c.Id, Username = c.User1Id == userId ? c.User1.Username : c.User2.Username })
            .SingleOrDefaultAsync();

        return row is null
            ? throw new BadRequestException(localizer.T("Photo.NeedPartner"))
            : (row.Id, row.Username);
    }

    private PhotoDto ToDto(Photo photo, string uploadedByUsername) =>
        Signed(new PhotoDto(photo.Id, photo.FilePath, photo.ThumbnailPath, photo.MediumPath, photo.Caption, photo.TakenAt, uploadedByUsername, photo.CreatedAt));

    private PhotoDto Signed(PhotoDto dto) =>
        dto with
        {
            Url = urlSigner.Sign(dto.Url),
            ThumbnailUrl = urlSigner.Sign(dto.ThumbnailUrl),
            MediumUrl = urlSigner.Sign(dto.MediumUrl),
        };
}
