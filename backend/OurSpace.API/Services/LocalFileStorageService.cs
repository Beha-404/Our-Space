namespace OurSpace.API.Services;

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

    public Task<Stream> OpenReadAsync(string url)
    {
        Stream stream = File.OpenRead(GetPhysicalPath(url));
        return Task.FromResult(stream);
    }

    public void Delete(string url)
    {
        var path = GetPhysicalPath(url);
        if (File.Exists(path))
            File.Delete(path);
    }

    public long GetSizeBytes(string url)
    {
        var file = new FileInfo(GetPhysicalPath(url));
        return file.Exists ? file.Length : 0;
    }

    private string GetPhysicalPath(string url)
    {
        var relative = url.TrimStart('/').Replace("uploads/", "", StringComparison.OrdinalIgnoreCase).TrimStart('/');
        return Path.Combine(_uploadsRoot, relative);
    }
}
