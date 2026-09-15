using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Wishlist;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public class WishlistService(AppDbContext db, ILocalizer localizer, ICoupleContext coupleContext, INotificationService notifications) : IWishlistService
{
    private const int MaxTitleLength = 200;

    public async Task<WishDto> CreateAsync(int userId, CreateWishRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new BadRequestException(localizer.T("Wish.TitleRequired"));

        if (request.Title.Trim().Length > MaxTitleLength)
            throw new BadRequestException(localizer.T("Wish.TitleTooLong"));

        var (coupleId, username) = await GetCoupleAndUsernameOrThrow(userId);

        var wish = new WishlistItem
        {
            CoupleId = coupleId,
            CreatedByUserId = userId,
            Title = request.Title.Trim(),
        };

        db.WishlistItems.Add(wish);
        await db.SaveChangesAsync();

        var partnerId = await coupleContext.GetPartnerUserIdAsync(userId);
        if (partnerId is not null)
            await notifications.NotifyAsync(partnerId.Value, userId, NotificationType.WishAdded, "wish", wish.Id, wish.Title);

        return ToDto(wish, username);
    }

    public async Task<List<WishDto>> GetAllAsync(int userId)
    {
        var coupleId = await coupleContext.GetCoupleIdOrThrow(userId, "Wish.NeedPartner");

        return await db.WishlistItems
            .Where(w => w.CoupleId == coupleId)
            .OrderBy(w => w.IsFulfilled)
            .ThenByDescending(w => w.CreatedAt)
            .Select(w => new WishDto(w.Id, w.Title, w.IsFulfilled, w.FulfilledAt, w.CreatedByUser.Username, w.CreatedAt))
            .ToListAsync();
    }

    public async Task<WishDto> ToggleFulfilledAsync(int userId, int wishId)
    {
        var wish = await GetWishOrThrow(userId, wishId);

        wish.IsFulfilled = !wish.IsFulfilled;
        wish.FulfilledAt = wish.IsFulfilled ? DateTime.UtcNow : null;
        await db.SaveChangesAsync();

        return ToDto(wish, wish.CreatedByUser.Username);
    }

    public async Task DeleteAsync(int userId, int wishId)
    {
        var wish = await GetWishOrThrow(userId, wishId);

        db.WishlistItems.Remove(wish);
        await db.SaveChangesAsync();
    }

    private async Task<WishlistItem> GetWishOrThrow(int userId, int wishId)
    {
        var coupleId = await coupleContext.GetCoupleIdOrThrow(userId, "Wish.NeedPartner");

        var wish = await db.WishlistItems.Include(w => w.CreatedByUser).SingleOrDefaultAsync(w => w.Id == wishId)
            ?? throw new NotFoundException(localizer.T("Wish.NotFound"));

        if (wish.CoupleId != coupleId)
            throw new NotFoundException(localizer.T("Wish.NotFound"));

        return wish;
    }

    private async Task<(int CoupleId, string Username)> GetCoupleAndUsernameOrThrow(int userId)
    {
        var row = await db.Couples
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .Select(c => new { c.Id, Username = c.User1Id == userId ? c.User1.Username : c.User2.Username })
            .SingleOrDefaultAsync();

        return row is null
            ? throw new BadRequestException(localizer.T("Wish.NeedPartner"))
            : (row.Id, row.Username);
    }

    private static WishDto ToDto(WishlistItem wish, string createdByUsername) =>
        new(wish.Id, wish.Title, wish.IsFulfilled, wish.FulfilledAt, createdByUsername, wish.CreatedAt);
}
