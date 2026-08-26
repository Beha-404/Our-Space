using OurSpace.API.Models.DTOs.Auth;

namespace OurSpace.API.Services;

public interface IInviteService
{
    Task<InviteDto> CreateAsync(int userId);

    Task<List<InviteDto>> GetMineAsync(int userId);

    Task RevokeAsync(int userId, int inviteId);

    Task<List<PendingAccountDto>> GetPendingAsync(int userId);

    Task ApproveAsync(int userId, string code);
}
