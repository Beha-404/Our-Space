namespace OurSpace.API.Options;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string ApiKey { get; set; } = "";
    public string FromAddress { get; set; } = "onboarding@resend.dev";
    public string FromName { get; set; } = "OurSpace";

    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
}
