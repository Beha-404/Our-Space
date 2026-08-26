namespace OurSpace.API.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public long QuotaBytesPerCouple { get; set; } = 5L * 1024 * 1024 * 1024;

    public int MaxImageDimension { get; set; } = 2000;

    public int ImageQuality { get; set; } = 82;
}
