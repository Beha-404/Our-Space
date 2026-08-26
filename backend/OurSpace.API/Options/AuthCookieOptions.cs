using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace OurSpace.API.Options;

public class AuthCookieOptions
{
    public const string SectionName = "AuthCookie";

    public bool Secure { get; set; }
    public SameSiteMode SameSite { get; set; } = SameSiteMode.Lax;

    public string? Domain { get; set; }
}
