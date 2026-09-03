using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace OurSpace.API.Services;

public class AzureBlobFileStorageService : IFileStorageService
{
    private const string ContainerName = "uploads";

    private readonly BlobContainerClient container;

    public AzureBlobFileStorageService(string connectionString)
    {
        container = new BlobContainerClient(connectionString, ContainerName);
    }

    public Uri? GenerateReadSasUri(string url, DateTimeOffset expiresOn, string? contentDisposition)
    {
        var blobClient = container.GetBlobClient(ToBlobName(url));

        if (!blobClient.CanGenerateSasUri)
            return null;

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = container.Name,
            BlobName = blobClient.Name,
            Resource = "b",
            ExpiresOn = expiresOn,
            ContentDisposition = contentDisposition,
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return blobClient.GenerateSasUri(sasBuilder);
    }

    public async Task<string> SaveAsync(Stream content, string subfolder, string fileExtension)
    {
        var blobName = $"{subfolder}/{Guid.NewGuid():N}{fileExtension}";

        if (content.CanSeek)
            content.Position = 0;

        await container.GetBlobClient(blobName).UploadAsync(content, overwrite: true);

        return $"/uploads/{blobName}";
    }

    public async Task<Stream> OpenReadAsync(string url)
    {
        var download = await container.GetBlobClient(ToBlobName(url)).DownloadStreamingAsync();
        return download.Value.Content;
    }

    public void Delete(string url)
    {
        container.GetBlobClient(ToBlobName(url)).DeleteIfExists();
    }

    public long GetSizeBytes(string url)
    {
        var blobClient = container.GetBlobClient(ToBlobName(url));
        return blobClient.Exists() ? blobClient.GetProperties().Value.ContentLength : 0;
    }

    private static string ToBlobName(string url) =>
        url.TrimStart('/').Replace("uploads/", "", StringComparison.OrdinalIgnoreCase).TrimStart('/');
}
