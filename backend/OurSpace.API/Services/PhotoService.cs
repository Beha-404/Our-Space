using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Memory;
using OurSpace.API.Models.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace OurSpace.API.Services;

public class PhotoService(AppDbContext db, IFileStorageService fileStorage) : IPhotoService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024;
    private const int ThumbnailWidth = 480;

    public async Task<PhotoDto> UploadAsync(int userId, IFormFile file, DateOnly takenAt, string? caption)
    {
        ValidateFile(file);

        var couple = await GetCoupleOrThrow(userId);
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension)) extension = ".jpg";

        await using var uploadStream = file.OpenReadStream();
        var filePath = await fileStorage.SaveAsync(uploadStream, "photos", extension);

        var thumbnailPath = await GenerateThumbnailAsync(filePath);

        var photo = new Photo
        {
            CoupleId = couple.Id,
            UploadedByUserId = userId,
            FilePath = filePath,
            ThumbnailPath = thumbnailPath,
            Caption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim(),
            TakenAt = takenAt,
        };

        db.Photos.Add(photo);
        await db.SaveChangesAsync();

        var uploader = await db.Users.SingleAsync(u => u.Id == userId);
        return ToDto(photo, uploader.Username);
    }

    public async Task<List<PhotoDto>> GetAllAsync(int userId)
    {
        var couple = await GetCoupleOrThrow(userId);

        return await db.Photos
            .Include(p => p.UploadedByUser)
            .Where(p => p.CoupleId == couple.Id)
            .OrderByDescending(p => p.TakenAt)
            .Select(p => new PhotoDto(p.Id, p.FilePath, p.ThumbnailPath, p.Caption, p.TakenAt, p.UploadedByUser.Username, p.CreatedAt))
            .ToListAsync();
    }

    public async Task DeleteAsync(int userId, int photoId)
    {
        var couple = await GetCoupleOrThrow(userId);

        var photo = await db.Photos.SingleOrDefaultAsync(p => p.Id == photoId)
            ?? throw new NotFoundException("Slika nije pronađena.");

        if (photo.CoupleId != couple.Id)
            throw new BadRequestException("Nemaš pristup ovoj slici.");

        fileStorage.Delete(photo.FilePath);
        fileStorage.Delete(photo.ThumbnailPath);

        db.Photos.Remove(photo);
        await db.SaveChangesAsync();
    }

    private void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
            throw new BadRequestException("Fajl je prazan.");

        if (file.Length > MaxFileSizeBytes)
            throw new BadRequestException("Slika je prevelika (maksimalno 10MB).");

        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new BadRequestException("Nepodržan format slike. Dozvoljeno: JPEG, PNG, WEBP, GIF.");
    }

    private async Task<string> GenerateThumbnailAsync(string originalUrl)
    {
        var originalPath = fileStorage.GetPhysicalPath(originalUrl);
        using var image = await Image.LoadAsync(originalPath);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(ThumbnailWidth, ThumbnailWidth),
        }));

        await using var thumbStream = new MemoryStream();
        await image.SaveAsJpegAsync(thumbStream);
        thumbStream.Position = 0;

        return await fileStorage.SaveAsync(thumbStream, "thumbnails", ".jpg");
    }

    private async Task<Couple> GetCoupleOrThrow(int userId) =>
        await db.Couples.SingleOrDefaultAsync(c => c.User1Id == userId || c.User2Id == userId)
        ?? throw new BadRequestException("Moraš biti uparen/a sa partnerom da bi dodavao/la slike.");

    private static PhotoDto ToDto(Photo photo, string uploadedByUsername) =>
        new(photo.Id, photo.FilePath, photo.ThumbnailPath, photo.Caption, photo.TakenAt, uploadedByUsername, photo.CreatedAt);
}
