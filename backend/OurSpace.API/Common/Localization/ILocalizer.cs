namespace OurSpace.API.Common.Localization;

public interface ILocalizer
{
    string CurrentLang { get; }
    string T(string key);
    string T(string key, params object[] args);
    string For(string key, string? lang);
    string For(string key, string? lang, params object[] args);
}
