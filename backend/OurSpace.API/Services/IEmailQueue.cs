namespace OurSpace.API.Services;

public record QueuedEmail(string ToEmail, string Subject, string Body);

public interface IEmailQueue
{
    void Enqueue(string toEmail, string subject, string body);
}
