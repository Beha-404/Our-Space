using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public interface IAttemptLimiter
{
    Task EnsureAllowedAsync(string action, string identifier, int limit, TimeSpan window);
}

public class AttemptLimiter(AppDbContext db, ILocalizer localizer) : IAttemptLimiter
{
    public async Task EnsureAllowedAsync(string action, string identifier, int limit, TimeSpan window)
    {
        var now = DateTime.UtcNow;
        var windowStart = new DateTime(now.Ticks - now.Ticks % window.Ticks, DateTimeKind.Utc);
        var bucket = $"{action}|{identifier}";

        var count = await IncrementAsync(bucket, windowStart);

        if (count <= limit)
            return;

        var retryAfter = (int)Math.Ceiling((windowStart + window - now).TotalSeconds);
        throw new TooManyAttemptsException(localizer.T("Generic.TooManyRequests"), Math.Max(retryAfter, 1));
    }

    private async Task<int> IncrementAsync(string bucket, DateTime windowStart)
    {
        var updated = await db.AttemptCounters
            .Where(c => c.Bucket == bucket && c.WindowStart == windowStart)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Count, c => c.Count + 1));

        if (updated == 0)
        {
            try
            {
                db.AttemptCounters.Add(new AttemptCounter { Bucket = bucket, WindowStart = windowStart, Count = 1 });
                await db.SaveChangesAsync();
                return 1;
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();
                await db.AttemptCounters
                    .Where(c => c.Bucket == bucket && c.WindowStart == windowStart)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.Count, c => c.Count + 1));
            }
        }

        return await db.AttemptCounters
            .Where(c => c.Bucket == bucket && c.WindowStart == windowStart)
            .Select(c => c.Count)
            .SingleAsync();
    }
}
