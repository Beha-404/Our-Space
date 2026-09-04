using System.IO.Compression;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;
using OurSpace.API.Middleware;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public class MemoriesExportJobHandler(
    AppDbContext db,
    IFileStorageService fileStorage,
    IFileUrlSigner urlSigner,
    IEmailQueue emailQueue,
    ILocalizer localizer) : IBackgroundJobHandler
{
    public string JobType => BackgroundJobTypes.MemoriesExport;

    public async Task HandleAsync(BackgroundJob job, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<MemoriesExportPayload>(job.Payload)
            ?? throw new InvalidOperationException("Export payload could not be read.");

        var photos = await db.Photos
            .Where(p => p.CoupleId == payload.CoupleId)
            .Select(p => new { p.FilePath, p.Caption, p.TakenAt })
            .ToListAsync(cancellationToken);

        var audio = await db.AudioMessages
            .Where(a => a.CoupleId == payload.CoupleId && a.Status == AudioStatus.Ready)
            .Select(a => new { a.FilePath, a.Caption, a.RecordedAt })
            .ToListAsync(cancellationToken);

        var tempPath = Path.GetTempFileName();

        try
        {
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await using (var fileStream = File.Create(tempPath))
            {
                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);

                foreach (var photo in photos)
                {
                    var name = BuildEntryName("Slike", photo.TakenAt.ToString("yyyy-MM-dd"), photo.Caption, photo.FilePath, usedNames);
                    await AddEntryAsync(archive, name, photo.FilePath);
                }

                foreach (var item in audio)
                {
                    var name = BuildEntryName("Audio poruke", item.RecordedAt.ToString("yyyy-MM-dd"), item.Caption, item.FilePath, usedNames);
                    await AddEntryAsync(archive, name, item.FilePath);
                }
            }

            await using var zipStream = File.OpenRead(tempPath);
            var stored = await fileStorage.SaveAsync(zipStream, "exports", ".zip");

            job.Result = JsonSerializer.Serialize(new { stored.Path, stored.SizeBytes });

            await NotifyRequesterAsync(job.RequestedByUserId, stored, cancellationToken);
        }
        finally
        {
            try { File.Delete(tempPath); } catch { /* best-effort cleanup */ }
        }
    }

    private async Task NotifyRequesterAsync(int userId, StoredFile stored, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.Email, u.PreferredLanguage })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
            return;

        var link = urlSigner.Sign(stored.Path);
        var megabytes = Math.Max(stored.SizeBytes / (1024 * 1024), 1);

        var subject = localizer.For("Email.ExportReady.Subject", user.PreferredLanguage);
        var body = localizer.For("Email.ExportReady.Body", user.PreferredLanguage, megabytes, link);

        await emailQueue.EnqueueAsync(user.Email, subject, body);
    }

    private async Task AddEntryAsync(ZipArchive archive, string entryName, string filePath)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.NoCompression);
        await using var entryStream = entry.Open();
        await using var sourceStream = await fileStorage.OpenReadAsync(filePath);
        await sourceStream.CopyToAsync(entryStream);
    }

    private static string BuildEntryName(string folder, string datePrefix, string? caption, string filePath, HashSet<string> usedNames)
    {
        var niceName = $"{datePrefix} {SignedFileMiddleware.SafeFileName(caption, filePath)}";
        var extension = Path.GetExtension(niceName);
        var stem = Path.GetFileNameWithoutExtension(niceName);

        var candidate = $"{folder}/{niceName}";
        var suffix = 1;
        while (!usedNames.Add(candidate))
            candidate = $"{folder}/{stem} ({++suffix}){extension}";

        return candidate;
    }
}
