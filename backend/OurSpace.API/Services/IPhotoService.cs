using Microsoft.AspNetCore.Http;
using OurSpace.API.Models.DTOs.Memory;

namespace OurSpace.API.Services;

public interface IPhotoService
{
    Task<PhotoDto> UploadAsync(int userId, IFormFile file, DateOnly takenAt, string? caption);
    Task<PagedResult<PhotoDto>> GetAllAsync(int userId, int page, int pageSize);
    Task<int> GetCountAsync(int userId);
    Task DeleteAsync(int userId, int photoId);
}
