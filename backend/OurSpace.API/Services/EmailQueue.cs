using OurSpace.API.Data;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public class EmailQueue(AppDbContext db) : IEmailQueue
{
    public async Task EnqueueAsync(string toEmail, string subject, string body)
    {
        db.OutboxEmails.Add(new OutboxEmail
        {
            ToEmail = toEmail,
            Subject = subject,
            Body = body,
        });

        await db.SaveChangesAsync();
    }
}
