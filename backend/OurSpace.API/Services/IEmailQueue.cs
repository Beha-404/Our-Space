namespace OurSpace.API.Services;

public interface IEmailQueue
{
    Task EnqueueAsync(string toEmail, string subject, string body);
}
