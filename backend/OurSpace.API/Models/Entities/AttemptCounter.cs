namespace OurSpace.API.Models.Entities;

public class AttemptCounter
{
    public int Id { get; set; }

    public required string Bucket { get; set; }
    public DateTime WindowStart { get; set; }
    public int Count { get; set; }
}
