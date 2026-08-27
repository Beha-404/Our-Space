namespace OurSpace.API.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public long QuotaBytesPerCouple { get; set; } = 5L * 1024 * 1024 * 1024;
}
