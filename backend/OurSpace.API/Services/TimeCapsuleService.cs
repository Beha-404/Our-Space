using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Capsule;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public class TimeCapsuleService(
    AppDbContext db,
    ILocalizer localizer,
    ICoupleContext coupleContext,
    INotificationService notifications) : ITimeCapsuleService
{
    private const int MaxTitleLength = 200;
    private const int MaxMessageLength = 5000;
    private static readonly TimeSpan MaxHorizon = TimeSpan.FromDays(365 * 50);

    public async Task<CapsuleDto> CreateAsync(int userId, CreateCapsuleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new BadRequestException(localizer.T("Capsule.TitleRequired"));

        if (request.Title.Trim().Length > MaxTitleLength)
            throw new BadRequestException(localizer.T("Capsule.TitleTooLong"));

        if (string.IsNullOrWhiteSpace(request.Message))
            throw new BadRequestException(localizer.T("Capsule.MessageRequired"));

        if (request.Message.Trim().Length > MaxMessageLength)
            throw new BadRequestException(localizer.T("Capsule.MessageTooLong"));

        if (request.OpenAt is DateTime openAt)
        {
            if (openAt <= DateTime.UtcNow)
                throw new BadRequestException(localizer.T("Capsule.OpenDateInPast"));

            if (openAt > DateTime.UtcNow + MaxHorizon)
                throw new BadRequestException(localizer.T("Capsule.OpenDateTooFar"));
        }

        var (coupleId, username) = await GetCoupleAndUsernameOrThrow(userId);

        var capsule = new TimeCapsule
        {
            CoupleId = coupleId,
            CreatedByUserId = userId,
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            OpenAt = request.OpenAt,
        };

        db.TimeCapsules.Add(capsule);
        await db.SaveChangesAsync();

        var partnerId = await coupleContext.GetPartnerUserIdAsync(userId);
        if (partnerId is not null)
        {
            await notifications.NotifyAsync(
                partnerId.Value, userId, NotificationType.CapsuleSealed, "capsule", capsule.Id, capsule.Title);
        }

        return ToDto(capsule, username);
    }

    public async Task<List<CapsuleDto>> GetAllAsync(int userId)
    {
        var coupleId = await coupleContext.GetCoupleIdOrThrow(userId, "Capsule.NeedPartner");

        var capsules = await db.TimeCapsules
            .Include(c => c.CreatedByUser)
            .Where(c => c.CoupleId == coupleId)
            .OrderBy(c => c.OpenedAt != null)
            .ThenBy(c => c.OpenAt ?? DateTime.MaxValue)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync();

        return capsules.Select(c => ToDto(c, c.CreatedByUser.Username)).ToList();
    }

    public async Task<CapsuleDto> GetAsync(int userId, int capsuleId)
    {
        var capsule = await GetCapsuleOrThrow(userId, capsuleId);

        if (!capsule.IsUnlocked)
            throw new BadRequestException(localizer.T("Capsule.StillSealed"));

        return ToDto(capsule, capsule.CreatedByUser.Username);
    }

    public async Task<CapsuleDto> OpenAsync(int userId, int capsuleId)
    {
        var capsule = await GetCapsuleOrThrow(userId, capsuleId);

        if (capsule.OpenAt is DateTime openAt && openAt > DateTime.UtcNow)
            throw new BadRequestException(localizer.T("Capsule.StillSealed"));

        if (capsule.OpenedAt is null)
        {
            capsule.OpenedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        return ToDto(capsule, capsule.CreatedByUser.Username);
    }

    public async Task DeleteAsync(int userId, int capsuleId)
    {
        var capsule = await GetCapsuleOrThrow(userId, capsuleId);

        if (capsule.CreatedByUserId != userId)
            throw new BadRequestException(localizer.T("Capsule.CannotDelete"));

        db.TimeCapsules.Remove(capsule);
        await db.SaveChangesAsync();
    }

    private async Task<TimeCapsule> GetCapsuleOrThrow(int userId, int capsuleId)
    {
        var coupleId = await coupleContext.GetCoupleIdOrThrow(userId, "Capsule.NeedPartner");

        var capsule = await db.TimeCapsules
            .Include(c => c.CreatedByUser)
            .SingleOrDefaultAsync(c => c.Id == capsuleId)
            ?? throw new NotFoundException(localizer.T("Capsule.NotFound"));

        if (capsule.CoupleId != coupleId)
            throw new NotFoundException(localizer.T("Capsule.NotFound"));

        return capsule;
    }

    private async Task<(int CoupleId, string Username)> GetCoupleAndUsernameOrThrow(int userId)
    {
        var row = await db.Couples
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .Select(c => new { c.Id, Username = c.User1Id == userId ? c.User1.Username : c.User2.Username })
            .SingleOrDefaultAsync();

        return row is null
            ? throw new BadRequestException(localizer.T("Capsule.NeedPartner"))
            : (row.Id, row.Username);
    }

    private static CapsuleDto ToDto(TimeCapsule capsule, string createdByUsername) =>
        new(
            capsule.Id,
            capsule.Title,
            capsule.IsUnlocked ? capsule.Message : null,
            capsule.OpenAt,
            capsule.OpenedAt,
            capsule.IsUnlocked,
            capsule.OpenAt is null && capsule.OpenedAt is null,
            createdByUsername,
            capsule.CreatedAt);
}
