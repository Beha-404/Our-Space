namespace OurSpace.API.Options;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string ApiKey { get; set; } = "";
    public string FromAddress { get; set; } = "onboarding@resend.dev";
}
