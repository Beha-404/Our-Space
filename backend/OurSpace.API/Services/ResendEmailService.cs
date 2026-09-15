using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using OurSpace.API.Options;

namespace OurSpace.API.Services;

public class ResendEmailService(HttpClient httpClient, IOptions<EmailOptions> options, ILogger<ResendEmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(string toEmail, string subject, string body)
    {
        try
        {
            var payload = new
            {
                from = _options.FromAddress,
                to = new[] { toEmail },
                subject,
                text = body,
                html = EmailTemplate.Render(subject, body),
            };

            var response = await httpClient.PostAsJsonAsync("emails", payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogWarning(
                    "Resend email to {To} failed with {Status}: {Error}",
                    toEmail, response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To} via Resend", toEmail);
        }
    }
}
