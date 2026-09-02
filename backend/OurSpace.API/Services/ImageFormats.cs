using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;

namespace OurSpace.API.Services;

public static class ImageFormats
{
    private const int MaxPixels = 50_000_000;

    private static readonly Dictionary<string, string> ExtensionByFormat = new(StringComparer.OrdinalIgnoreCase)
    {
        ["JPEG"] = ".jpg",
        ["PNG"] = ".png",
        ["WEBP"] = ".webp",
        ["GIF"] = ".gif",
    };

    /// <summary>
    /// Decides the stored extension from the file's actual decoded content, never from the
    /// client-supplied name. Returns null when the upload is not a supported image.
    /// </summary>
    public static async Task<string?> ResolveExtensionAsync(IFormFile file)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            var info = await Image.IdentifyAsync(stream);

            if ((long)info.Width * info.Height > MaxPixels)
                return null;

            return ExtensionByFormat.TryGetValue(info.Metadata.DecodedImageFormat?.Name ?? "", out var extension)
                ? extension
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
