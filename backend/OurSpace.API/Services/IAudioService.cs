using Microsoft.AspNetCore.Http;
using OurSpace.API.Models.DTOs.Memory;

namespace OurSpace.API.Services;

public interface IAudioService
{
    Task<AudioDto> UploadAsync(int userId, IFormFile file, DateOnly recordedAt, string? caption);
    Task<List<AudioDto>> GetAllAsync(int userId);
    Task DeleteAsync(int userId, int audioId);
}
