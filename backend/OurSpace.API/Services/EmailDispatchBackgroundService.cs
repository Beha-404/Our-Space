namespace OurSpace.API.Services;

public class EmailDispatchBackgroundService(
    EmailQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<EmailDispatchBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var email in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                await emailService.SendAsync(email.ToEmail, email.Subject, email.Body);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to dispatch queued email to {To}", email.ToEmail);
            }
        }
    }
}
