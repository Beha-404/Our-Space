using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;

namespace OurSpace.API.Services;

public interface ICoupleContext
{
    Task<int> GetCoupleIdOrThrow(int userId, string notPairedMessageKey);
    Task<int?> GetPartnerUserIdAsync(int userId);
}

public class CoupleContext(AppDbContext db, ILocalizer localizer) : ICoupleContext
{
    private int? resolved;
    private int? resolvedPartnerId;
    private bool partnerResolved;

    public async Task<int> GetCoupleIdOrThrow(int userId, string notPairedMessageKey)
    {
        resolved ??= await db.Couples
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .Select(c => (int?)c.Id)
            .SingleOrDefaultAsync();

        return resolved ?? throw new BadRequestException(localizer.T(notPairedMessageKey));
    }

    public async Task<int?> GetPartnerUserIdAsync(int userId)
    {
        if (!partnerResolved)
        {
            resolvedPartnerId = await db.Couples
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .Select(c => (int?)(c.User1Id == userId ? c.User2Id : c.User1Id))
                .SingleOrDefaultAsync();
            partnerResolved = true;
        }

        return resolvedPartnerId;
    }
}
