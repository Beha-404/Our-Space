namespace OurSpace.API.Services;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string subfolder, string fileExtension);

    void Delete(string url);

    string GetPhysicalPath(string url);

    long GetSizeBytes(string url);
}
