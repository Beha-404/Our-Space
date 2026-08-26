namespace OurSpace.API.Common.Localization;

public class Localizer(IHttpContextAccessor httpContextAccessor) : ILocalizer
{
    public static readonly string[] SupportedLangs = ["bs", "en", "es"];
    public const string DefaultLang = "bs";

    public string CurrentLang => ResolveFromHeader();

    public string T(string key) => For(key, CurrentLang);

    public string T(string key, params object[] args) => For(key, CurrentLang, args);

    public string For(string key, string? lang)
    {
        var normalized = Normalize(lang);
        if (Messages.All.TryGetValue(key, out var translations))
        {
            if (translations.TryGetValue(normalized, out var text)) return text;
            if (translations.TryGetValue(DefaultLang, out var fallback)) return fallback;
        }
        return key;
    }

    public string For(string key, string? lang, params object[] args) =>
        string.Format(For(key, lang), args);

    public static string Normalize(string? lang) =>
        !string.IsNullOrWhiteSpace(lang) && SupportedLangs.Contains(lang) ? lang : DefaultLang;

    private string ResolveFromHeader()
    {
        var header = httpContextAccessor.HttpContext?.Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrWhiteSpace(header)) return DefaultLang;

        var primary = header.Split(',')[0].Split(';')[0].Trim().Split('-')[0].ToLowerInvariant();
        return Normalize(primary);
    }
}
