using Microsoft.EntityFrameworkCore;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Memory;

namespace OurSpace.API.Services;

public interface IMemoryFeedService
{
    Task<MemoryFeedDto> GetPageAsync(int userId, MemoryFeedQuery query);
}

public record MemoryFeedQuery(
    int Page = 1,
    int PageSize = 24,
    string Sort = "newest",
    int? Year = null,
    int? Month = null,
    string? Type = null);

public class MemoryFeedService(
    AppDbContext db,
    ICoupleContext coupleContext,
    IFileUrlSigner urlSigner) : IMemoryFeedService
{
    private const int MaxPageSize = 60;

    public async Task<MemoryFeedDto> GetPageAsync(int userId, MemoryFeedQuery query)
    {
        var coupleId = await coupleContext.GetCoupleIdOrThrow(userId, "Photo.NeedPartner");

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var feed = BuildFeedQuery(coupleId, query);

        feed = query.Sort == "oldest"
            ? feed.OrderBy(m => m.Date).ThenBy(m => m.Id)
            : feed.OrderByDescending(m => m.Date).ThenByDescending(m => m.Id);

        var items = await feed
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .ToListAsync();

        var hasMore = items.Count > pageSize;
        if (hasMore) items.RemoveAt(items.Count - 1);

        foreach (var item in items)
        {
            item.Url = urlSigner.Sign(item.Url);
            item.ThumbnailUrl = urlSigner.Sign(item.ThumbnailUrl);
            item.MediumUrl = urlSigner.Sign(item.MediumUrl);
        }

        var years = page == 1 ? await GetYearsAsync(coupleId) : null;

        return new MemoryFeedDto(items, hasMore, years);
    }

    private IQueryable<MemoryRow> BuildFeedQuery(int coupleId, MemoryFeedQuery query)
    {
        var photos = db.Photos
            .Where(p => p.CoupleId == coupleId)
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
            });

        var audio = db.AudioMessages
            .Where(a => a.CoupleId == coupleId)
            .Select(a => new MemoryRow
            {
                Id = a.Id,
                Type = "audio",
                Url = a.FilePath,
                ThumbnailUrl = null,
                MediumUrl = null,
                Caption = a.Caption,
                Date = a.RecordedAt,
                UploadedByUsername = a.UploadedByUser.Username,
                CreatedAt = a.CreatedAt,
            });

        var feed = query.Type switch
        {
            "photo" => photos,
            "audio" => audio,
            _ => photos.Concat(audio),
        };

        if (query.Year is int year)
            feed = feed.Where(m => m.Date.Year == year);

        if (query.Month is int month)
            feed = feed.Where(m => m.Date.Month == month);

        return feed;
    }

    private async Task<List<int>> GetYearsAsync(int coupleId)
    {
        var photoYears = db.Photos.Where(p => p.CoupleId == coupleId).Select(p => p.TakenAt.Year);
        var audioYears = db.AudioMessages.Where(a => a.CoupleId == coupleId).Select(a => a.RecordedAt.Year);

        return await photoYears.Concat(audioYears)
            .Distinct()
            .OrderByDescending(year => year)
            .ToListAsync();
    }
}
