namespace OurSpace.API.Services;

/// <summary>
/// Placeholder IEmailService that logs instead of sending. Swap the DI registration in
/// Program.cs for a real provider (e.g. Resend, Brevo) once credentials are configured.
/// </summary>
public class LoggingEmailService(ILogger<LoggingEmailService> logger) : IEmailService
{
    public Task SendAsync(string toEmail, string subject, string body)
    {
        logger.LogInformation("[EMAIL-STUB] To: {To} | Subject: {Subject}\n{Body}", toEmail, subject, body);
        return Task.CompletedTask;
    }
}
