using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Memory;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public class AudioService(AppDbContext db, IFileStorageService fileStorage) : IAudioService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "audio/mpeg", "audio/wav", "audio/x-wav", "audio/ogg", "audio/webm", "audio/mp4", "audio/x-m4a", "audio/aac"
    };

    private const long MaxFileSizeBytes = 20 * 1024 * 1024;

    public async Task<AudioDto> UploadAsync(int userId, IFormFile file, DateOnly recordedAt, string? caption)
    {
        ValidateFile(file);

        var couple = await GetCoupleOrThrow(userId);
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension)) extension = ".mp3";

        await using var uploadStream = file.OpenReadStream();
        var filePath = await fileStorage.SaveAsync(uploadStream, "audio", extension);

        var audio = new AudioMessage
        {
            CoupleId = couple.Id,
            UploadedByUserId = userId,
            FilePath = filePath,
            Caption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim(),
            RecordedAt = recordedAt,
        };

        db.AudioMessages.Add(audio);
        await db.SaveChangesAsync();

        var uploader = await db.Users.SingleAsync(u => u.Id == userId);
        return ToDto(audio, uploader.Username);
    }

    public async Task<List<AudioDto>> GetAllAsync(int userId)
    {
        var couple = await GetCoupleOrThrow(userId);

        return await db.AudioMessages
            .Include(a => a.UploadedByUser)
            .Where(a => a.CoupleId == couple.Id)
            .OrderByDescending(a => a.RecordedAt)
            .Select(a => new AudioDto(a.Id, a.FilePath, a.Caption, a.RecordedAt, a.UploadedByUser.Username, a.CreatedAt))
            .ToListAsync();
    }

    public async Task DeleteAsync(int userId, int audioId)
    {
        var couple = await GetCoupleOrThrow(userId);

        var audio = await db.AudioMessages.SingleOrDefaultAsync(a => a.Id == audioId)
            ?? throw new NotFoundException("Audio poruka nije pronađena.");

        if (audio.CoupleId != couple.Id)
            throw new BadRequestException("Nemaš pristup ovoj audio poruci.");

        fileStorage.Delete(audio.FilePath);

        db.AudioMessages.Remove(audio);
        await db.SaveChangesAsync();
    }

    private void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
            throw new BadRequestException("Fajl je prazan.");

        if (file.Length > MaxFileSizeBytes)
            throw new BadRequestException("Audio fajl je prevelik (maksimalno 20MB).");

        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new BadRequestException("Nepodržan format audio fajla.");
    }

    private async Task<Couple> GetCoupleOrThrow(int userId) =>
        await db.Couples.SingleOrDefaultAsync(c => c.User1Id == userId || c.User2Id == userId)
        ?? throw new BadRequestException("Moraš biti uparen/a sa partnerom da bi dodavao/la audio poruke.");

    private static AudioDto ToDto(AudioMessage audio, string uploadedByUsername) =>
        new(audio.Id, audio.FilePath, audio.Caption, audio.RecordedAt, uploadedByUsername, audio.CreatedAt);
}
