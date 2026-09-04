namespace OurSpace.API.Models.Entities;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string? ProfilePictureUrl { get; set; }

    public string? PairingCode { get; set; }
    public DateTime? PairingCodeExpiresAt { get; set; }

    public string? PendingEmail { get; set; }
    public string? EmailChangeCode { get; set; }
    public DateTime? EmailChangeCodeExpiresAt { get; set; }

    public string? PasswordResetCode { get; set; }
    public DateTime? PasswordResetCodeExpiresAt { get; set; }
    public int PasswordResetAttempts { get; set; }

    public string? LoginCode { get; set; }
    public DateTime? LoginCodeExpiresAt { get; set; }
    public int LoginCodeAttempts { get; set; }

    public string PreferredLanguage { get; set; } = "bs";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
