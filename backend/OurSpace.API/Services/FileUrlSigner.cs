using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using OurSpace.API.Options;

namespace OurSpace.API.Services;

public class FileUrlSigner(IOptions<JwtOptions> jwtOptions) : IFileUrlSigner
{
    private static readonly TimeSpan LinkLifetime = TimeSpan.FromHours(2);
    private const long BucketSeconds = 3600;
    private const string Purpose = "ourspace-file-access";

    private readonly byte[] key = Encoding.UTF8.GetBytes(jwtOptions.Value.Key);

    [return: NotNullIfNotNull(nameof(path))]
    public string? Sign(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        var deadline = DateTimeOffset.UtcNow.Add(LinkLifetime).ToUnixTimeSeconds();
        var expires = (deadline / BucketSeconds * BucketSeconds).ToString();

        return $"{path}?exp={expires}&sig={Compute(path, expires)}";
    }

    public bool IsValid(string path, string? expires, string? signature)
    {
        if (string.IsNullOrEmpty(expires) || string.IsNullOrEmpty(signature))
            return false;

        if (!long.TryParse(expires, out var unixSeconds))
            return false;

        if (DateTimeOffset.FromUnixTimeSeconds(unixSeconds) < DateTimeOffset.UtcNow)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Compute(path, expires)),
            Encoding.UTF8.GetBytes(signature));
    }

    private string Compute(string path, string expires)
    {
        var payload = Encoding.UTF8.GetBytes($"{Purpose}|{path}|{expires}");
        var hash = HMACSHA256.HashData(key, payload);

        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
