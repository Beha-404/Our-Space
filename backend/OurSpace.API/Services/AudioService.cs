using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Memory;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public class AudioService(
    AppDbContext db,
    IFileStorageService fileStorage,
    ILocalizer localizer,
    IFileUrlSigner urlSigner,
    IStorageQuotaService quota,
    IFFmpegReadiness ffmpeg,
    IBackgroundJobQueue jobQueue,
    ICoupleContext coupleContext) : IAudioService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "audio/mpeg", "audio/wav", "audio/x-wav", "audio/ogg", "audio/webm", "audio/mp4", "audio/x-m4a", "audio/aac",
        "video/mp4"
    };

    private static readonly HashSet<string> VideoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4"
    };

    private const long MaxFileSizeBytes = 20 * 1024 * 1024;
    private const int MaxPageSize = 500;
    private const int MaxCaptionLength = 300;

    public async Task<AudioDto> UploadAsync(int userId, IFormFile file, DateOnly recordedAt, string? caption)
    {
        ValidateFile(file);

        if (string.IsNullOrWhiteSpace(caption))
            throw new BadRequestException(localizer.T("Audio.TitleRequired"));

        if (caption.Trim().Length > MaxCaptionLength)
            throw new BadRequestException(localizer.T("Audio.CaptionTooLong"));

        var (coupleId, username) = await GetCoupleAndUsernameOrThrow(userId);
        var isVideo = IsVideoUpload(file);

        if (isVideo && !ffmpeg.IsReady)
            throw new BadRequestException(localizer.T("Audio.ConverterNotReady"));

        await quota.EnsureRoomAsync(coupleId, file.Length);

        var extension = isVideo ? ".mp4" : Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension)) extension = ".mp3";

        StoredFile stored;
        await using (var uploadStream = file.OpenReadStream())
        {
            stored = await fileStorage.SaveAsync(uploadStream, "audio", extension);
        }

        var audio = new AudioMessage
        {
            CoupleId = coupleId,
            UploadedByUserId = userId,
            FilePath = stored.Path,
            SizeBytes = stored.SizeBytes,
            Status = isVideo ? AudioStatus.Processing : AudioStatus.Ready,
            Caption = caption.Trim(),
            RecordedAt = recordedAt,
        };

        db.AudioMessages.Add(audio);
        await db.SaveChangesAsync();

        if (isVideo)
        {
            await jobQueue.EnqueueAsync(
                BackgroundJobTypes.AudioConversion,
                new AudioConversionPayload(audio.Id, stored.Path),
                userId);
        }

        return ToDto(audio, username);
    }

    public async Task<PagedResult<AudioDto>> GetAllAsync(int userId, int page, int pageSize)
    {
        var coupleId = await coupleContext.GetCoupleIdOrThrow(userId, "Audio.NeedPartner");

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var audio = await db.AudioMessages
            .Where(a => a.CoupleId == coupleId)
            .OrderByDescending(a => a.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(a => new AudioDto(a.Id, a.FilePath, a.Caption, a.RecordedAt, a.UploadedByUser.Username, a.CreatedAt))
            .ToListAsync();

        var hasMore = audio.Count > pageSize;
        if (hasMore) audio.RemoveAt(audio.Count - 1);

        return new PagedResult<AudioDto>(audio.Select(Signed).ToList(), hasMore);
    }

    public async Task<int> GetCountAsync(int userId)
    {
        var coupleId = await coupleContext.GetCoupleIdOrThrow(userId, "Audio.NeedPartner");
        return await db.AudioMessages.CountAsync(a => a.CoupleId == coupleId);
    }

    public async Task DeleteAsync(int userId, int audioId)
    {
        var coupleId = await coupleContext.GetCoupleIdOrThrow(userId, "Audio.NeedPartner");

        var audio = await db.AudioMessages.SingleOrDefaultAsync(a => a.Id == audioId)
            ?? throw new NotFoundException(localizer.T("Audio.NotFound"));

        if (audio.CoupleId != coupleId)
            throw new NotFoundException(localizer.T("Audio.NotFound"));

        db.AudioMessages.Remove(audio);
        await db.SaveChangesAsync();
        await ClearQuotaWarningAsync(coupleId);

        await fileStorage.DeleteAsync(audio.FilePath);
    }

    private void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
            throw new BadRequestException(localizer.T("Audio.FileEmpty"));

        if (file.Length > MaxFileSizeBytes)
            throw new BadRequestException(localizer.T("Audio.TooLarge"));

        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new BadRequestException(localizer.T("Audio.UnsupportedFormat"));
    }

    private static bool IsVideoUpload(IFormFile file) =>
        VideoContentTypes.Contains(file.ContentType) ||
        string.Equals(Path.GetExtension(file.FileName), ".mp4", StringComparison.OrdinalIgnoreCase);

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
            ? throw new BadRequestException(localizer.T("Audio.NeedPartner"))
            : (row.Id, row.Username);
    }

    private AudioDto ToDto(AudioMessage audio, string uploadedByUsername) =>
        Signed(new AudioDto(audio.Id, audio.FilePath, audio.Caption, audio.RecordedAt, uploadedByUsername, audio.CreatedAt));

    private AudioDto Signed(AudioDto dto) =>
        dto with { Url = urlSigner.Sign(dto.Url) };
}
