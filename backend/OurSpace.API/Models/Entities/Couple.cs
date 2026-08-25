namespace OurSpace.API.Models.Entities;

public class Couple
{
    public int Id { get; set; }

    public int User1Id { get; set; }
    public User User1 { get; set; } = null!;

    public int User2Id { get; set; }
    public User User2 { get; set; } = null!;

    public DateOnly? RelationshipStartDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
