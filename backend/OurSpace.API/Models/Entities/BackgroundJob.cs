namespace OurSpace.API.Models.Entities;

public enum BackgroundJobStatus
{
    Pending = 0,
    Running = 1,
    Done = 2,
    Failed = 3,
}

public class BackgroundJob
{
    public int Id { get; set; }

    public required string Type { get; set; }
    public required string Payload { get; set; }

    public BackgroundJobStatus Status { get; set; } = BackgroundJobStatus.Pending;
    public int Attempts { get; set; }

    public int RequestedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public string? Result { get; set; }
    public string? LastError { get; set; }
}
