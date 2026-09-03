using System.Net;
using System.Text.Json;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;

namespace OurSpace.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException ex)
        {
            logger.LogInformation("{Status} {Method} {Path}: {Message}",
                (int)ex.StatusCode, context.Request.Method, context.Request.Path, ex.Message);

            await WriteProblem(context, ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            var localizer = context.RequestServices.GetRequiredService<ILocalizer>();
            await WriteProblem(context, HttpStatusCode.InternalServerError, localizer.T("Generic.UnexpectedError"));
        }
    }

    private static Task WriteProblem(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var problem = new { title = message, status = (int)statusCode };
        return context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
