namespace OurSpace.API.Services;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string subfolder, string fileExtension);

    Task<Stream> OpenReadAsync(string url);

    void Delete(string url);

    long GetSizeBytes(string url);
}
