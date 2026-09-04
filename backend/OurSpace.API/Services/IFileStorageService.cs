namespace OurSpace.API.Services;

public record StoredFile(string Path, long SizeBytes);

public interface IFileStorageService
{
    Task<StoredFile> SaveAsync(Stream content, string subfolder, string fileExtension);

    Task<Stream> OpenReadAsync(string url);

    Task DeleteAsync(string url);
}
