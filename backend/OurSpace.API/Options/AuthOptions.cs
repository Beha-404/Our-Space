namespace OurSpace.API.Options;

public class AuthOptions
{
    public const string SectionName = "Auth";

    public bool TwoFactorEnabled { get; set; } = true;
}
