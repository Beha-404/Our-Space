using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
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

internal static class EmailTemplate
{
    private static readonly Regex CodeLine = new(@"^(?<label>.*:)\s*(?<code>\d{4,8})$", RegexOptions.Compiled);

    public static string Render(string subject, string plainBody)
    {
        var content = new StringBuilder();
        foreach (var paragraph in plainBody.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var match = CodeLine.Match(paragraph);
            if (match.Success)
            {
                content.Append($"""<p style="margin:0 0 8px;font-size:14px;color:#8a7c6c;font-family:Arial,Helvetica,sans-serif;">{WebUtility.HtmlEncode(match.Groups["label"].Value)}</p>""");
                content.Append($"""<div style="margin:0 0 24px;padding:16px 24px;background:#2b2018;border:1px dashed #4a3b2c;border-radius:12px;text-align:center;font-size:32px;font-weight:600;letter-spacing:8px;color:#f3ece2;font-family:Georgia,'Times New Roman',serif;">{WebUtility.HtmlEncode(match.Groups["code"].Value)}</div>""");
            }
            else
            {
                var encoded = WebUtility.HtmlEncode(paragraph).Replace("\n", "<br>");
                content.Append($"""<p style="margin:0 0 16px;font-size:15px;line-height:1.6;color:#b3a494;font-family:Arial,Helvetica,sans-serif;">{encoded}</p>""");
            }
        }

        return $"""
            <!DOCTYPE html>
            <html>
              <body style="margin:0;padding:0;background-color:#1d1712;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#1d1712;padding:40px 20px;">
                  <tr>
                    <td align="center">
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:480px;background-color:#241c15;border:1px solid #3a2e22;border-radius:16px;overflow:hidden;">
                        <tr>
                          <td style="padding:28px 32px;text-align:center;border-bottom:1px solid #3a2e22;">
                            <span style="font-size:18px;color:#c1734a;font-family:Georgia,serif;">&#10022;</span>
                            <span style="font-size:18px;font-weight:700;color:#c1734a;margin-left:6px;font-family:Georgia,'Times New Roman',serif;">OurSpace</span>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:32px;">
                            <h1 style="margin:0 0 20px;font-size:20px;font-weight:600;color:#f3ece2;font-family:Georgia,'Times New Roman',serif;">{WebUtility.HtmlEncode(subject)}</h1>
                            {content}
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:18px 32px 26px;border-top:1px solid #3a2e22;">
                            <p style="margin:0;font-size:12px;color:#8a7c6c;font-family:Arial,Helvetica,sans-serif;">Ovaj mail je automatski poslan sa OurSpace aplikacije.</p>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>
              </body>
            </html>
            """;
    }
}
