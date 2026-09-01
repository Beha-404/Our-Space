using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Memory;
using OurSpace.API.Models.Entities;
using Xabe.FFmpeg;

namespace OurSpace.API.Services;

public class AudioService(
    AppDbContext db,
    IFileStorageService fileStorage,
    ILocalizer localizer,
    IFileUrlSigner urlSigner,
    IStorageQuotaService quota) : IAudioService
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

    public async Task<AudioDto> UploadAsync(int userId, IFormFile file, DateOnly recordedAt, string? caption)
    {
        ValidateFile(file);

        if (string.IsNullOrWhiteSpace(caption))
            throw new BadRequestException(localizer.T("Audio.TitleRequired"));

        var (coupleId, username) = await GetCoupleAndUsernameOrThrow(userId);
        var isVideo = IsVideoUpload(file);

        await quota.EnsureRoomAsync(coupleId, file.Length);

        string filePath;

        if (isVideo)
        {
            string videoUrl;
            await using (var uploadStream = file.OpenReadStream())
            {
                videoUrl = await fileStorage.SaveAsync(uploadStream, "audio", ".mp4");
            }

            try
            {
                filePath = await ConvertToMp3Async(videoUrl);
            }
            finally
            {
                fileStorage.Delete(videoUrl);
            }
        }
        else
        {
            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension)) extension = ".mp3";

            await using var uploadStream = file.OpenReadStream();
            filePath = await fileStorage.SaveAsync(uploadStream, "audio", extension);
        }

        var audio = new AudioMessage
        {
            CoupleId = coupleId,
            UploadedByUserId = userId,
            FilePath = filePath,
            SizeBytes = fileStorage.GetSizeBytes(filePath),
            Caption = caption.Trim(),
            RecordedAt = recordedAt,
        };

        db.AudioMessages.Add(audio);
        await db.SaveChangesAsync();

        return ToDto(audio, username);
    }

    public async Task<PagedResult<AudioDto>> GetAllAsync(int userId, int page, int pageSize)
    {
        var coupleId = await GetCoupleIdOrThrow(userId);

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

    public async Task DeleteAsync(int userId, int audioId)
    {
        var coupleId = await GetCoupleIdOrThrow(userId);

        var audio = await db.AudioMessages.SingleOrDefaultAsync(a => a.Id == audioId)
            ?? throw new NotFoundException(localizer.T("Audio.NotFound"));

        if (audio.CoupleId != coupleId)
            throw new NotFoundException(localizer.T("Audio.NotFound"));

        fileStorage.Delete(audio.FilePath);

        db.AudioMessages.Remove(audio);
        await db.SaveChangesAsync();
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

    private async Task<string> ConvertToMp3Async(string videoUrl)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var videoPath = Path.Combine(tempDir, "input.mp4");
            var mp3Path = Path.Combine(tempDir, "output.mp3");

            await using (var videoStream = await fileStorage.OpenReadAsync(videoUrl))
            await using (var videoFile = File.Create(videoPath))
            {
                await videoStream.CopyToAsync(videoFile);
            }

            try
            {
                var conversion = FFmpeg.Conversions.New()
                    .AddParameter($"-i \"{videoPath}\"")
                    .AddParameter("-vn -acodec libmp3lame -q:a 2")
                    .SetOutput(mp3Path);

                await conversion.Start();
            }
            catch (Exception)
            {
                throw new BadRequestException(localizer.T("Audio.ConversionFailed"));
            }

            await using var mp3Stream = File.OpenRead(mp3Path);
            return await fileStorage.SaveAsync(mp3Stream, "audio", ".mp3");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private async Task<int> GetCoupleIdOrThrow(int userId)
    {
        var id = await db.Couples
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .Select(c => (int?)c.Id)
            .SingleOrDefaultAsync();

        return id ?? throw new BadRequestException(localizer.T("Audio.NeedPartner"));
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
