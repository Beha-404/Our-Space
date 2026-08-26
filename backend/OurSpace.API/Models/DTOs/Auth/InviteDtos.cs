namespace OurSpace.API.Models.DTOs.Auth;

public record InviteDto(
    int Id,
    string Code,
    DateTime ExpiresAt,
    bool IsUsable,
    string? UsedByUsername,
    DateTime? UsedAt
);

public record PendingAccountDto(int UserId, string Username, string Email, DateTime RequestedAt);

public record ApproveAccountRequest(string Code);
