using Microsoft.AspNetCore.Http;
using OurSpace.API.Models.DTOs.Memory;

namespace OurSpace.API.Services;

public interface IAudioService
{
    Task<AudioDto> UploadAsync(int userId, IFormFile file, DateOnly recordedAt, string? caption);
    Task<PagedResult<AudioDto>> GetAllAsync(int userId, int page, int pageSize);
    Task DeleteAsync(int userId, int audioId);
}
