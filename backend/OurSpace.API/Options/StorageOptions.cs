namespace OurSpace.API.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public long QuotaBytesPerCouple { get; set; } = 5L * 1024 * 1024 * 1024;

    public long ExportInlineLimitBytes { get; set; } = 200L * 1024 * 1024;
}
