namespace OurSpace.API.Services;

public class LoggingEmailService(ILogger<LoggingEmailService> logger) : IEmailService
{
    public Task SendAsync(string toEmail, string subject, string body)
    {
        logger.LogInformation("[EMAIL-STUB] To: {To} | Subject: {Subject}\n{Body}", toEmail, subject, body);
        return Task.CompletedTask;
    }
}
