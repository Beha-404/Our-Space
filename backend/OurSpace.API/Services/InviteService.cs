using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Auth;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public class InviteService(AppDbContext db, ILocalizer localizer) : IInviteService
{
    private static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(7);
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 8;
    private const int MaxOpenInvites = 10;

    public async Task<InviteDto> CreateAsync(int userId)
    {
        var openInvites = await db.Invites
            .CountAsync(i => i.CreatedByUserId == userId && i.UsedAt == null && i.RevokedAt == null && i.ExpiresAt > DateTime.UtcNow);

        if (openInvites >= MaxOpenInvites)
            throw new BadRequestException(localizer.T("Invite.TooMany", MaxOpenInvites));

        var invite = new Invite
        {
            Code = await GenerateUniqueCodeAsync(),
            CreatedByUserId = userId,
            ExpiresAt = DateTime.UtcNow.Add(InviteLifetime),
        };

        db.Invites.Add(invite);
        await db.SaveChangesAsync();

        return new InviteDto(invite.Id, invite.Code, invite.ExpiresAt, true, null, null);
    }

    public async Task<List<InviteDto>> GetMineAsync(int userId)
    {
        var invites = await db.Invites
            .Where(i => i.CreatedByUserId == userId && i.RevokedAt == null)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new
            {
                i.Id,
                i.Code,
                i.ExpiresAt,
                i.UsedAt,
                i.RevokedAt,
                UsedByUsername = db.Users.Where(u => u.Id == i.UsedByUserId).Select(u => u.Username).FirstOrDefault(),
            })
            .ToListAsync();

        return invites
            .Select(i => new InviteDto(
                i.Id,
                i.Code,
                i.ExpiresAt,
                i.UsedAt is null && i.RevokedAt is null && i.ExpiresAt > DateTime.UtcNow,
                i.UsedByUsername,
                i.UsedAt))
            .ToList();
    }

    public async Task RevokeAsync(int userId, int inviteId)
    {
        var invite = await db.Invites.SingleOrDefaultAsync(i => i.Id == inviteId && i.CreatedByUserId == userId)
            ?? throw new NotFoundException(localizer.T("Invite.NotFound"));

        if (invite.UsedAt is not null)
            throw new BadRequestException(localizer.T("Invite.AlreadyUsed"));

        invite.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<List<PendingAccountDto>> GetPendingAsync(int userId)
    {
        return await db.Invites
            .Where(i => i.CreatedByUserId == userId && i.UsedByUserId != null)
            .Join(db.Users.Where(u => !u.IsApproved), i => i.UsedByUserId, u => u.Id, (i, u) => new PendingAccountDto(u.Id, u.Username, u.Email, u.CreatedAt))
            .OrderBy(p => p.RequestedAt)
            .ToListAsync();
    }

    public async Task ApproveAsync(int userId, string code)
    {
        var trimmed = code.Trim();

        var invite = await db.Invites
            .Where(i => i.CreatedByUserId == userId && i.UsedByUserId != null)
            .OrderByDescending(i => i.UsedAt)
            .ToListAsync();

        var pendingIds = invite.Select(i => i.UsedByUserId!.Value).ToList();

        var user = await db.Users
            .Where(u => pendingIds.Contains(u.Id) && !u.IsApproved)
            .SingleOrDefaultAsync(u => u.ApprovalCode == trimmed);

        if (user is null || user.ApprovalCodeExpiresAt < DateTime.UtcNow)
            throw new BadRequestException(localizer.T("Invite.InvalidApprovalCode"));

        user.IsApproved = true;
        user.ApprovalCode = null;
        user.ApprovalCodeExpiresAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    private async Task<string> GenerateUniqueCodeAsync()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = string.Concat(Enumerable.Range(0, CodeLength)
                .Select(_ => CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)]));

            if (!await db.Invites.AnyAsync(i => i.Code == code))
                return code;
        }

        throw new InvalidOperationException("Could not generate a unique invite code.");
    }
}
