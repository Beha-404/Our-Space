using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using OurSpace.API.Common;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;
using OurSpace.API.Middleware;
using OurSpace.API.Options;
using OurSpace.API.Services;

var builder = WebApplication.CreateBuilder(args);

const string AngularDevCorsPolicy = "AngularDev";

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(10),
        errorNumbersToAdd: null)));

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.Configure<AuthCookieOptions>(builder.Configuration.GetSection(AuthCookieOptions.SectionName));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        var localizer = context.HttpContext.RequestServices.GetRequiredService<ILocalizer>();
        var problem = System.Text.Json.JsonSerializer.Serialize(new
        {
            title = localizer.T("Generic.TooManyRequests"),
            status = (int)HttpStatusCode.TooManyRequests,
        });

        await context.HttpContext.Response.WriteAsync(problem, token);
    };

    options.AddPolicy(RateLimitPolicies.Login, http =>
        RateLimitPartition.GetFixedWindowLimiter(ClientKey(http), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(5),
        }));

    options.AddPolicy(RateLimitPolicies.ForgotPassword, http =>
        RateLimitPartition.GetFixedWindowLimiter(ClientKey(http), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromHours(1),
        }));

    options.AddPolicy(RateLimitPolicies.ResetPassword, http =>
        RateLimitPartition.GetFixedWindowLimiter(ClientKey(http), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 15,
            Window = TimeSpan.FromHours(1),
        }));

    options.AddPolicy(RateLimitPolicies.VerifyLogin, http =>
        RateLimitPartition.GetFixedWindowLimiter(ClientKey(http), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromHours(1),
        }));

    options.AddPolicy(RateLimitPolicies.Writes, http =>
        RateLimitPartition.GetFixedWindowLimiter(ClientKey(http), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromHours(1),
        }));

    options.AddPolicy(RateLimitPolicies.Pair, http =>
        RateLimitPartition.GetFixedWindowLimiter(ClientKey(http), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromHours(1),
        }));

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
        RateLimitPartition.GetFixedWindowLimiter(ClientKey(http), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 300,
            Window = TimeSpan.FromMinutes(1),
        }));

    static string ClientKey(HttpContext http) =>
        http.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
        ?? http.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ILocalizer, Localizer>();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEventService, EventService>();
var blobConnectionString = builder.Configuration["Storage:BlobConnectionString"];
if (!string.IsNullOrWhiteSpace(blobConnectionString))
{
    builder.Services.AddSingleton<IFileStorageService>(new AzureBlobFileStorageService(blobConnectionString));
}
else
{
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
}
builder.Services.AddSingleton<IFileUrlSigner, FileUrlSigner>();
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.AddScoped<IStorageQuotaService, StorageQuotaService>();
builder.Services.AddSingleton<IFFmpegReadiness, FFmpegReadiness>();
builder.Services.AddHostedService<FFmpegSetupBackgroundService>();
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddScoped<IAudioService, AudioService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
var emailOptions = builder.Configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();

if (!string.IsNullOrWhiteSpace(emailOptions.SmtpHost))
{
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
}
else if (!string.IsNullOrWhiteSpace(emailOptions.ApiKey))
{
    builder.Services.AddHttpClient<ResendEmailService>(client =>
    {
        client.BaseAddress = new Uri("https://api.resend.com/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", emailOptions.ApiKey);
    });
    builder.Services.AddScoped<IEmailService>(sp => sp.GetRequiredService<ResendEmailService>());
}
else
{
    builder.Services.AddSingleton<IEmailService, LoggingEmailService>();
}

builder.Services.AddSingleton<EmailQueue>();
builder.Services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<EmailQueue>());
builder.Services.AddHostedService<EmailDispatchBackgroundService>();

builder.Services.AddHostedService<EventReminderBackgroundService>();
builder.Services.AddHostedService<DeletedUserCleanupBackgroundService>();

var app = builder.Build();

var forwardedHeaderOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
forwardedHeaderOptions.KnownIPNetworks.Clear();
forwardedHeaderOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaderOptions);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors(AngularDevCorsPolicy);

var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

const string UploadsCacheControl = "public, max-age=3600, immutable";

app.UseMiddleware<SignedFileMiddleware>();

if (!string.IsNullOrWhiteSpace(blobConnectionString))
{
    var contentTypeProvider = new FileExtensionContentTypeProvider();

    app.MapGet("/uploads/{**path}", async (string path, HttpContext http, IFileStorageService storage) =>
    {
        if (!contentTypeProvider.TryGetContentType(path, out var contentType))
            contentType = "application/octet-stream";

        try
        {
            var stream = await storage.OpenReadAsync($"/uploads/{path}");
            http.Response.Headers.CacheControl = UploadsCacheControl;
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.Stream(stream, contentType);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return Results.NotFound();
        }
    });
}
else
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads",
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.CacheControl = UploadsCacheControl;
            ctx.Context.Response.Headers.XContentTypeOptions = "nosniff";
        },
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();

public partial class Program { }
