using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using OurSpace.API.Options;

namespace OurSpace.API.Services;

public class SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(string toEmail, string subject, string body)
    {
        try
        {
            using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPassword),
            };

            var from = string.IsNullOrWhiteSpace(_options.FromAddress) ? _options.SmtpUser : _options.FromAddress;

            using var message = new MailMessage
            {
                From = new MailAddress(from, _options.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
            };

            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                EmailTemplate.Render(subject, body), null, "text/html"));

            message.To.Add(toEmail);

            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To} over SMTP", toEmail);
        }
    }
}
