using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.StaticFiles;

namespace OurSpace.API.Services;

public class AzureBlobFileStorageService : IFileStorageService
{
    private const string ContainerName = "uploads";

    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

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

    public async Task<StoredFile> SaveAsync(Stream content, string subfolder, string fileExtension)
    {
        var blobName = $"{subfolder}/{Guid.NewGuid():N}{fileExtension}";

        if (content.CanSeek)
            content.Position = 0;

        ContentTypeProvider.TryGetContentType(fileExtension, out var contentType);
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType ?? "application/octet-stream" },
        };

        var blobClient = container.GetBlobClient(blobName);
        await blobClient.UploadAsync(content, options);

        var sizeBytes = content.CanSeek
            ? content.Length
            : (await blobClient.GetPropertiesAsync()).Value.ContentLength;

        return new StoredFile($"/uploads/{blobName}", sizeBytes);
    }

    public async Task<Stream> OpenReadAsync(string url)
    {
        var download = await container.GetBlobClient(ToBlobName(url)).DownloadStreamingAsync();
        return download.Value.Content;
    }

    public async Task DeleteAsync(string url)
    {
        await container.GetBlobClient(ToBlobName(url)).DeleteIfExistsAsync();
    }

    public async Task<(int Count, long Bytes)> DeleteOrphanedBlobsAsync(IReadOnlySet<string> keepBlobNames, bool dryRun)
    {
        var count = 0;
        long bytes = 0;

        await foreach (var blob in container.GetBlobsAsync())
        {
            if (keepBlobNames.Contains(blob.Name))
                continue;

            bytes += blob.Properties.ContentLength ?? 0;
            if (!dryRun)
                await container.DeleteBlobIfExistsAsync(blob.Name);
            count++;
        }

        return (count, bytes);
    }

    internal static string ToBlobName(string url) =>
        url.TrimStart('/').Replace("uploads/", "", StringComparison.OrdinalIgnoreCase).TrimStart('/');
}
