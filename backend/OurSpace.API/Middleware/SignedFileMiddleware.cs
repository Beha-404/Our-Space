using OurSpace.API.Services;

namespace OurSpace.API.Middleware;

public class SignedFileMiddleware(RequestDelegate next)
{
    private const string UploadsPrefix = "/uploads";

    public async Task InvokeAsync(HttpContext context, IFileUrlSigner signer)
    {
        if (!context.Request.Path.StartsWithSegments(UploadsPrefix))
        {
            await next(context);
            return;
        }

        var query = context.Request.Query;

        if (!signer.IsValid(context.Request.Path.Value!, query["exp"], query["sig"]))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (query.ContainsKey("download"))
        {
            var fileName = SafeFileName(query["name"], context.Request.Path.Value!);
            context.Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
        }

        await next(context);
    }

    internal static string SafeFileName(string? requested, string path)
    {
        var fallback = Path.GetFileName(path);

        if (string.IsNullOrWhiteSpace(requested))
            return fallback;

        var extension = Path.GetExtension(path);
        var stem = Path.GetFileNameWithoutExtension(requested.Trim());

        var cleaned = new string(stem
            .Select(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or ' ' ? c : '-')
            .ToArray())
            .Trim();

        cleaned = string.Join('-', cleaned.Split('-', StringSplitOptions.RemoveEmptyEntries));

        if (cleaned.Length > 60)
            cleaned = cleaned[..60];

        return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned + extension;
    }
}
