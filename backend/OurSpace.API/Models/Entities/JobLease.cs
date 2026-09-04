namespace OurSpace.API.Models.Entities;

public class JobLease
{
    public required string Name { get; set; }

    public string? Owner { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
