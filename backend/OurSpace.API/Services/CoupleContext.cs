using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;

namespace OurSpace.API.Services;

public interface ICoupleContext
{
    Task<int> GetCoupleIdOrThrow(int userId, string notPairedMessageKey);
}

public class CoupleContext(AppDbContext db, ILocalizer localizer) : ICoupleContext
{
    private int? resolved;

    public async Task<int> GetCoupleIdOrThrow(int userId, string notPairedMessageKey)
    {
        resolved ??= await db.Couples
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .Select(c => (int?)c.Id)
            .SingleOrDefaultAsync();

        return resolved ?? throw new BadRequestException(localizer.T(notPairedMessageKey));
    }
}
