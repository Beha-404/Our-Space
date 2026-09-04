namespace OurSpace.API.Services;

public class LocalFileStorageService(IWebHostEnvironment env) : IFileStorageService
{
    private readonly string _uploadsRoot = Path.Combine(env.ContentRootPath, "uploads");

    public async Task<StoredFile> SaveAsync(Stream content, string subfolder, string fileExtension)
    {
        var folder = Path.Combine(_uploadsRoot, subfolder);
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{fileExtension}";
        var fullPath = Path.Combine(folder, fileName);

        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream);
        }

        return new StoredFile($"/uploads/{subfolder}/{fileName}", new FileInfo(fullPath).Length);
    }

    public Task<Stream> OpenReadAsync(string url)
    {
        Stream stream = File.OpenRead(GetPhysicalPath(url));
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string url)
    {
        var path = GetPhysicalPath(url);
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }

    private string GetPhysicalPath(string url)
    {
        var relative = url.TrimStart('/').Replace("uploads/", "", StringComparison.OrdinalIgnoreCase).TrimStart('/');
        return Path.Combine(_uploadsRoot, relative);
    }
}
