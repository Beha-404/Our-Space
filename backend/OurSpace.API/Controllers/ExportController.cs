using System.IO.Compression;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OurSpace.API.Common;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;
using OurSpace.API.Middleware;
using OurSpace.API.Models.Entities;
using OurSpace.API.Options;
using OurSpace.API.Services;

namespace OurSpace.API.Controllers;

[ApiController]
[Route("api/export")]
[Authorize]
public class ExportController(
    AppDbContext db,
    ICoupleContext coupleContext,
    IFileStorageService fileStorage,
    IStorageQuotaService quota,
    IBackgroundJobQueue jobQueue,
    IOptions<StorageOptions> storageOptions,
    ILocalizer localizer) : ControllerBase
{
    [HttpGet("memories")]
    [EnableRateLimiting(RateLimitPolicies.Export)]
    public async Task<IActionResult> ExportMemories()
    {
        var userId = this.GetUserId();
        var coupleId = await coupleContext.GetCoupleIdOrThrow(userId, "Photo.NeedPartner");

        var photos = await db.Photos
            .Where(p => p.CoupleId == coupleId)
            .Select(p => new { p.FilePath, p.Caption, p.TakenAt })
            .ToListAsync();

        var audio = await db.AudioMessages
            .Where(a => a.CoupleId == coupleId && a.Status == AudioStatus.Ready)
            .Select(a => new { a.FilePath, a.Caption, a.RecordedAt })
            .ToListAsync();

        if (photos.Count == 0 && audio.Count == 0)
            throw new BadRequestException(localizer.T("Export.NothingToExport"));

        var totalBytes = await quota.GetUsedBytesAsync(coupleId);

        if (totalBytes > storageOptions.Value.ExportInlineLimitBytes)
            return await QueueExportAsync(userId, coupleId);

        var tempPath = Path.GetTempFileName();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using (var fileStream = System.IO.File.Create(tempPath))
        {
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);

            foreach (var photo in photos)
            {
                var name = BuildEntryName("Slike", photo.TakenAt.ToString("yyyy-MM-dd"), photo.Caption, photo.FilePath, usedNames);
                await AddEntryAsync(archive, name, photo.FilePath);
            }

            foreach (var item in audio)
            {
                var name = BuildEntryName("Glasovna pisma", item.RecordedAt.ToString("yyyy-MM-dd"), item.Caption, item.FilePath, usedNames);
                await AddEntryAsync(archive, name, item.FilePath);
            }
        }

        HttpContext.Response.OnCompleted(() =>
        {
            try { System.IO.File.Delete(tempPath); } catch { /* best-effort cleanup */ }
            return Task.CompletedTask;
        });

        return PhysicalFile(tempPath, "application/zip", $"ourspace-uspomene-{DateTime.UtcNow:yyyy-MM-dd}.zip");
    }

    private async Task<IActionResult> QueueExportAsync(int userId, int coupleId)
    {
        var alreadyQueued = await db.BackgroundJobs.AnyAsync(j =>
            j.Type == BackgroundJobTypes.MemoriesExport
            && j.RequestedByUserId == userId
            && (j.Status == BackgroundJobStatus.Pending || j.Status == BackgroundJobStatus.Running));

        if (alreadyQueued)
            throw new BadRequestException(localizer.T("Export.AlreadyRunning"));

        await jobQueue.EnqueueAsync(
            BackgroundJobTypes.MemoriesExport,
            new MemoriesExportPayload(coupleId),
            userId);

        return Accepted(new { queued = true });
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
