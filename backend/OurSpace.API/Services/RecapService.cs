using Microsoft.EntityFrameworkCore;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Memory;
using OurSpace.API.Models.DTOs.Recap;

namespace OurSpace.API.Services;

public interface IRecapService
{
    Task<RecapDto> GetAsync(int userId, int? year);
    Task<YearTeaserDto> GetTeaserAsync(int userId);
}

public class RecapService(
    AppDbContext db,
    ICoupleContext coupleContext,
    IFileUrlSigner urlSigner) : IRecapService
{
    private const int HighlightCount = 6;
    private const int TeaserPhotoCount = 4;
    private const int HighlightCandidatePool = 120;

    public async Task<RecapDto> GetAsync(int userId, int? year)
    {
        var coupleId = await coupleContext.GetCoupleIdOrThrow(userId, "Photo.NeedPartner");
        var targetYear = year ?? DateTime.UtcNow.Year;

        var (photoDates, audioDates) = await GetMemoryMonthsAsync(coupleId, targetYear);
        var perMonth = CountPerMonth(photoDates.Concat(audioDates));

        var events = await db.Events
            .CountAsync(e => e.CoupleId == coupleId && e.CancelledAt == null && e.EventDate.Year == targetYear);

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
            await GetHighlightsAsync(coupleId, targetYear, HighlightCount),
            await GetAvailableYearsAsync(coupleId));
    }

    public async Task<YearTeaserDto> GetTeaserAsync(int userId)
    {
        var coupleId = await coupleContext.GetCoupleIdOrThrow(userId, "Photo.NeedPartner");
        var year = DateTime.UtcNow.Year;

        var (photoDates, audioDates) = await GetMemoryMonthsAsync(coupleId, year);
        var perMonth = CountPerMonth(photoDates.Concat(audioDates));
        var total = perMonth.Sum();

        int? busiestMonth = total == 0 ? null : Array.LastIndexOf(perMonth, perMonth.Max()) + 1;

        var photos = await GetHighlightsAsync(coupleId, year, TeaserPhotoCount);
        var photoUrls = photos
            .Select(p => p.ThumbnailUrl ?? p.Url)
            .ToList();

        return new YearTeaserDto(year, total, busiestMonth, photoUrls);
    }

    private async Task<(List<int> Photos, List<int> Audio)> GetMemoryMonthsAsync(int coupleId, int year)
    {
        var photoMonths = await db.Photos
            .Where(p => p.CoupleId == coupleId && p.TakenAt.Year == year)
            .Select(p => p.TakenAt.Month)
            .ToListAsync();

        var audioMonths = await db.AudioMessages
            .Where(a => a.CoupleId == coupleId && a.RecordedAt.Year == year)
            .Select(a => a.RecordedAt.Month)
            .ToListAsync();

        return (photoMonths, audioMonths);
    }

    private static int[] CountPerMonth(IEnumerable<int> months)
    {
        var perMonth = new int[12];
        foreach (var month in months)
            perMonth[month - 1]++;

        return perMonth;
    }

    private async Task<List<MemoryRow>> GetHighlightsAsync(int coupleId, int year, int count)
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

        var highlights = SpreadAcrossYear(candidates, count);

        foreach (var item in highlights)
        {
            item.Url = urlSigner.Sign(item.Url);
            item.ThumbnailUrl = urlSigner.Sign(item.ThumbnailUrl);
            item.MediumUrl = urlSigner.Sign(item.MediumUrl);
        }

        return highlights;
    }

    private static List<MemoryRow> SpreadAcrossYear(List<MemoryRow> candidates, int count)
    {
        if (candidates.Count <= count)
            return candidates;

        var step = (double)candidates.Count / count;

        return Enumerable.Range(0, count)
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
