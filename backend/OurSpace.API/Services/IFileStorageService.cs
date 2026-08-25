namespace OurSpace.API.Services;

public interface IFileStorageService
{
    /// <summary>Saves a file under the given subfolder and returns its public URL path (e.g. "/uploads/photos/abc.jpg").</summary>
    Task<string> SaveAsync(Stream content, string subfolder, string fileExtension);

    void Delete(string url);

    string GetPhysicalPath(string url);
}
