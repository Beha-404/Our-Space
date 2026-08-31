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
    IStorageQuotaService quota) : IPhotoService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024;
    private const int ThumbnailWidth = 480;
    private const int MaxPageSize = 500;

    public async Task<PhotoDto> UploadAsync(int userId, IFormFile file, DateOnly takenAt, string? caption)
    {
        ValidateFile(file);

        var (coupleId, username) = await GetCoupleAndUsernameOrThrow(userId);
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension)) extension = ".jpg";

        await quota.EnsureRoomAsync(coupleId, file.Length);

        string filePath;

        await using (var uploadStream = file.OpenReadStream())
        {
            filePath = await fileStorage.SaveAsync(uploadStream, "photos", extension);
        }

        var thumbnailPath = await GenerateThumbnailAsync(filePath);
        var sizeBytes = fileStorage.GetSizeBytes(filePath) + fileStorage.GetSizeBytes(thumbnailPath);

        var photo = new Photo
        {
            CoupleId = coupleId,
            UploadedByUserId = userId,
            FilePath = filePath,
            ThumbnailPath = thumbnailPath,
            SizeBytes = sizeBytes,
            Caption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim(),
            TakenAt = takenAt,
        };

        db.Photos.Add(photo);
        await db.SaveChangesAsync();

        return ToDto(photo, username);
    }

    public async Task<PagedResult<PhotoDto>> GetAllAsync(int userId, int page, int pageSize)
    {
        var coupleId = await GetCoupleIdOrThrow(userId);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var photos = await db.Photos
            .Where(p => p.CoupleId == coupleId)
            .OrderByDescending(p => p.TakenAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(p => new PhotoDto(p.Id, p.FilePath, p.ThumbnailPath, p.Caption, p.TakenAt, p.UploadedByUser.Username, p.CreatedAt))
            .ToListAsync();

        var hasMore = photos.Count > pageSize;
        if (hasMore) photos.RemoveAt(photos.Count - 1);

        return new PagedResult<PhotoDto>(photos.Select(Signed).ToList(), hasMore);
    }

    public async Task DeleteAsync(int userId, int photoId)
    {
        var coupleId = await GetCoupleIdOrThrow(userId);

        var photo = await db.Photos.SingleOrDefaultAsync(p => p.Id == photoId)
            ?? throw new NotFoundException(localizer.T("Photo.NotFound"));

        if (photo.CoupleId != coupleId)
            throw new NotFoundException(localizer.T("Photo.NotFound"));

        fileStorage.Delete(photo.FilePath);
        fileStorage.Delete(photo.ThumbnailPath);

        db.Photos.Remove(photo);
        await db.SaveChangesAsync();
    }

    private void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
            throw new BadRequestException(localizer.T("Photo.FileEmpty"));

        if (file.Length > MaxFileSizeBytes)
            throw new BadRequestException(localizer.T("Photo.TooLarge"));

        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new BadRequestException(localizer.T("Photo.UnsupportedFormat"));
    }

    private async Task<string> GenerateThumbnailAsync(string originalUrl)
    {
        var originalPath = fileStorage.GetPhysicalPath(originalUrl);
        using var image = await Image.LoadAsync(originalPath);

        image.Mutate(x => x
            .AutoOrient()
            .Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(ThumbnailWidth, ThumbnailWidth),
            }));

        await using var thumbStream = new MemoryStream();
        await image.SaveAsJpegAsync(thumbStream);
        thumbStream.Position = 0;

        return await fileStorage.SaveAsync(thumbStream, "thumbnails", ".jpg");
    }

    private async Task<int> GetCoupleIdOrThrow(int userId)
    {
        var id = await db.Couples
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .Select(c => (int?)c.Id)
            .SingleOrDefaultAsync();

        return id ?? throw new BadRequestException(localizer.T("Photo.NeedPartner"));
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
        Signed(new PhotoDto(photo.Id, photo.FilePath, photo.ThumbnailPath, photo.Caption, photo.TakenAt, uploadedByUsername, photo.CreatedAt));

    private PhotoDto Signed(PhotoDto dto) =>
        dto with { Url = urlSigner.Sign(dto.Url), ThumbnailUrl = urlSigner.Sign(dto.ThumbnailUrl) };
}
