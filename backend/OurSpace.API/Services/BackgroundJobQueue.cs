using System.Text.Json;
using OurSpace.API.Data;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public interface IBackgroundJobQueue
{
    Task<BackgroundJob> EnqueueAsync<TPayload>(string type, TPayload payload, int requestedByUserId);
}

public class BackgroundJobQueue(AppDbContext db) : IBackgroundJobQueue
{
    public async Task<BackgroundJob> EnqueueAsync<TPayload>(string type, TPayload payload, int requestedByUserId)
    {
        var job = new BackgroundJob
        {
            Type = type,
            Payload = JsonSerializer.Serialize(payload),
            RequestedByUserId = requestedByUserId,
        };

        db.BackgroundJobs.Add(job);
        await db.SaveChangesAsync();

        return job;
    }
}
