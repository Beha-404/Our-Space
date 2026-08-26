using System.Diagnostics.CodeAnalysis;

namespace OurSpace.API.Services;

public interface IFileUrlSigner
{
    [return: NotNullIfNotNull(nameof(path))]
    string? Sign(string? path);

    bool IsValid(string path, string? expires, string? signature);
}
