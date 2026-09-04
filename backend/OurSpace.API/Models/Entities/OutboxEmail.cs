namespace OurSpace.API.Models.Entities;

public class OutboxEmail
{
    public int Id { get; set; }

    public required string ToEmail { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }

    public int Attempts { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public string? LastError { get; set; }
}
