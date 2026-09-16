using OurSpace.API.Models.DTOs.Capsule;

namespace OurSpace.API.Services;

public interface ITimeCapsuleService
{
    Task<CapsuleDto> CreateAsync(int userId, CreateCapsuleRequest request);
    Task<List<CapsuleDto>> GetAllAsync(int userId);
    Task<CapsuleDto> GetAsync(int userId, int capsuleId);
    Task<CapsuleDto> OpenAsync(int userId, int capsuleId);
    Task DeleteAsync(int userId, int capsuleId);
}
