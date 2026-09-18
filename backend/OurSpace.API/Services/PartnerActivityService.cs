using Microsoft.EntityFrameworkCore;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Home;

namespace OurSpace.API.Services;

public interface IPartnerActivityService
{
    Task<PartnerActivityDto> GetAsync(int userId);
}

public class PartnerActivityService(
    AppDbContext db,
    ICoupleContext coupleContext,
    IFileUrlSigner urlSigner) : IPartnerActivityService
{
    private const int RecentPhotoCount = 4;

    public async Task<PartnerActivityDto> GetAsync(int userId)
    {
        var coupleId = await coupleContext.GetCoupleIdOrThrow(userId, "Photo.NeedPartner");
        var partnerId = await coupleContext.GetPartnerUserIdAsync(userId) ?? 0;

        var photos = await db.Photos.CountAsync(p => p.CoupleId == coupleId && p.UploadedByUserId == partnerId);
        var voiceLetters = await db.AudioMessages.CountAsync(a => a.CoupleId == coupleId && a.UploadedByUserId == partnerId);
        var wishes = await db.WishlistItems.CountAsync(w => w.CoupleId == coupleId && w.CreatedByUserId == partnerId);
        var events = await db.Events.CountAsync(e => e.CoupleId == coupleId && e.CreatedByUserId == partnerId);
        var capsules = await db.TimeCapsules.CountAsync(c => c.CoupleId == coupleId && c.CreatedByUserId == partnerId);

        var recent = await db.Photos
            .Where(p => p.CoupleId == coupleId && p.UploadedByUserId == partnerId)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(RecentPhotoCount)
            .Select(p => new { p.Id, p.ThumbnailPath, p.Caption, p.TakenAt })
            .ToListAsync();

        var recentPhotos = recent
            .Select(p => new PartnerPhotoDto(p.Id, urlSigner.Sign(p.ThumbnailPath), p.Caption, p.TakenAt))
            .ToList();

        return new PartnerActivityDto(photos, voiceLetters, wishes, events, capsules, recentPhotos);
    }
}
