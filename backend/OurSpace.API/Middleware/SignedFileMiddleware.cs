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

        await next(context);
    }
}
