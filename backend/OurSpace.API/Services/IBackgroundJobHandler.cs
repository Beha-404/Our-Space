using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public interface IBackgroundJobHandler
{
    string JobType { get; }

    Task HandleAsync(BackgroundJob job, CancellationToken cancellationToken);
}

public static class BackgroundJobTypes
{
    public const string AudioConversion = "audio-conversion";
    public const string MemoriesExport = "memories-export";
}

public record AudioConversionPayload(int AudioId, string SourcePath);

public record MemoriesExportPayload(int CoupleId);
