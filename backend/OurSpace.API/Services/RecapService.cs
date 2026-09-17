using Microsoft.EntityFrameworkCore;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Memory;
using OurSpace.API.Models.DTOs.Recap;

namespace OurSpace.API.Services;

public interface IRecapService
{
    Task<RecapDto> GetAsync(int userId, int? year);
}

public class RecapService(
    AppDbContext db,
    ICoupleContext coupleContext,
    IFileUrlSigner urlSigner) : IRecapService
{
    private const int HighlightCount = 6;
    private const int HighlightCandidatePool = 120;

    public async Task<RecapDto> GetAsync(int userId, int? year)
    {
        var coupleId = await coupleContext.GetCoupleIdOrThrow(userId, "Photo.NeedPartner");
        var targetYear = year ?? DateTime.UtcNow.Year;

        var photoDates = await db.Photos
            .Where(p => p.CoupleId == coupleId && p.TakenAt.Year == targetYear)
            .Select(p => p.TakenAt.Month)
            .ToListAsync();

        var audioDates = await db.AudioMessages
            .Where(a => a.CoupleId == coupleId && a.RecordedAt.Year == targetYear)
            .Select(a => a.RecordedAt.Month)
            .ToListAsync();

        var perMonth = new int[12];
        foreach (var month in photoDates.Concat(audioDates))
            perMonth[month - 1]++;

        var events = await db.Events
            .CountAsync(e => e.CoupleId == coupleId && e.EventDate.Year == targetYear);

        var wishesFulfilled = await db.WishlistItems
            .CountAsync(w => w.CoupleId == coupleId && w.FulfilledAt != null && w.FulfilledAt!.Value.Year == targetYear);

        var capsulesSealed = await db.TimeCapsules
            .CountAsync(c => c.CoupleId == coupleId && c.CreatedAt.Year == targetYear);

        return new RecapDto(
            targetYear,
            photoDates.Count,
            audioDates.Count,
            events,
            wishesFulfilled,
            capsulesSealed,
            perMonth.ToList(),
            await GetHighlightsAsync(coupleId, targetYear),
            await GetAvailableYearsAsync(coupleId));
    }

    private async Task<List<MemoryRow>> GetHighlightsAsync(int coupleId, int year)
    {
        var candidates = await db.Photos
            .Where(p => p.CoupleId == coupleId && p.TakenAt.Year == year)
            .OrderBy(p => p.TakenAt)
            .Take(HighlightCandidatePool)
            .Select(p => new MemoryRow
            {
                Id = p.Id,
                Type = "photo",
                Url = p.FilePath,
                ThumbnailUrl = p.ThumbnailPath,
                MediumUrl = p.MediumPath,
                Caption = p.Caption,
                Date = p.TakenAt,
                UploadedByUsername = p.UploadedByUser.Username,
                CreatedAt = p.CreatedAt,
            })
            .ToListAsync();

        var highlights = SpreadAcrossYear(candidates);

        foreach (var item in highlights)
        {
            item.Url = urlSigner.Sign(item.Url);
            item.ThumbnailUrl = urlSigner.Sign(item.ThumbnailUrl);
            item.MediumUrl = urlSigner.Sign(item.MediumUrl);
        }

        return highlights;
    }

    private static List<MemoryRow> SpreadAcrossYear(List<MemoryRow> candidates)
    {
        if (candidates.Count <= HighlightCount)
            return candidates;

        var step = (double)candidates.Count / HighlightCount;

        return Enumerable.Range(0, HighlightCount)
            .Select(i => candidates[(int)(i * step)])
            .ToList();
    }

    private async Task<List<int>> GetAvailableYearsAsync(int coupleId)
    {
        var photoYears = db.Photos.Where(p => p.CoupleId == coupleId).Select(p => p.TakenAt.Year);
        var audioYears = db.AudioMessages.Where(a => a.CoupleId == coupleId).Select(a => a.RecordedAt.Year);

        return await photoYears.Concat(audioYears)
            .Distinct()
            .OrderByDescending(year => year)
            .ToListAsync();
    }
}
