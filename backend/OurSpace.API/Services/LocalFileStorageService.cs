namespace OurSpace.API.Services;

/// <summary>
/// Saves uploaded files to a local "uploads" folder next to the app. Swap this
/// implementation (behind IFileStorageService) for a cloud provider (e.g. Cloudflare R2)
/// when deploying online — nothing else in the app needs to change.
/// </summary>
public class LocalFileStorageService(IWebHostEnvironment env) : IFileStorageService
{
    private readonly string _uploadsRoot = Path.Combine(env.ContentRootPath, "uploads");

    public async Task<string> SaveAsync(Stream content, string subfolder, string fileExtension)
    {
        var folder = Path.Combine(_uploadsRoot, subfolder);
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{fileExtension}";
        var fullPath = Path.Combine(folder, fileName);

        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream);
        }

        return $"/uploads/{subfolder}/{fileName}";
    }

    public void Delete(string url)
    {
        var path = GetPhysicalPath(url);
        if (File.Exists(path))
            File.Delete(path);
    }

    public string GetPhysicalPath(string url)
    {
        var relative = url.TrimStart('/').Replace("uploads/", "", StringComparison.OrdinalIgnoreCase).TrimStart('/');
        return Path.Combine(_uploadsRoot, relative);
    }
}
