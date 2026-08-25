using Microsoft.AspNetCore.Http;
using OurSpace.API.Models.DTOs.Memory;

namespace OurSpace.API.Services;

public interface IPhotoService
{
    Task<PhotoDto> UploadAsync(int userId, IFormFile file, DateOnly takenAt, string? caption);
    Task<List<PhotoDto>> GetAllAsync(int userId);
    Task DeleteAsync(int userId, int photoId);
}
