using System.IO.Compression;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;
using OurSpace.API.Middleware;
using OurSpace.API.Services;

namespace OurSpace.API.Controllers;

[ApiController]
[Route("api/export")]
[Authorize]
public class ExportController(AppDbContext db, IFileStorageService fileStorage, ILocalizer localizer) : ControllerBase
{
    [HttpGet("memories")]
    [EnableRateLimiting(RateLimitPolicies.Export)]
    public async Task<IActionResult> ExportMemories()
    {
        var coupleId = await GetCoupleIdOrThrow(this.GetUserId());

        var photos = await db.Photos
            .Where(p => p.CoupleId == coupleId)
            .Select(p => new { p.FilePath, p.Caption, p.TakenAt })
            .ToListAsync();

        var audio = await db.AudioMessages
            .Where(a => a.CoupleId == coupleId)
            .Select(a => new { a.FilePath, a.Caption, a.RecordedAt })
            .ToListAsync();

        if (photos.Count == 0 && audio.Count == 0)
            throw new BadRequestException(localizer.T("Export.NothingToExport"));

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
                var name = BuildEntryName("Audio poruke", item.RecordedAt.ToString("yyyy-MM-dd"), item.Caption, item.FilePath, usedNames);
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

    private async Task<int> GetCoupleIdOrThrow(int userId)
    {
        var id = await db.Couples
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .Select(c => (int?)c.Id)
            .SingleOrDefaultAsync();

        return id ?? throw new BadRequestException(localizer.T("Photo.NeedPartner"));
    }
}
