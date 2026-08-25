using Microsoft.AspNetCore.Http;
using OurSpace.API.Models.DTOs.User;

namespace OurSpace.API.Services;

public interface IUserService
{
    Task<UserDto> GetCurrentAsync(int userId);
    Task<UserDto> UpdateAsync(int userId, UpdateUserRequest request);
    Task<UserDto> UpdateProfilePictureAsync(int userId, IFormFile file);
    Task DeleteAsync(int userId);
    Task<PairingCodeResponse> GeneratePairingCodeAsync(int userId);
    Task<UserDto> PairAsync(int userId, PairRequest request);
    Task<UserDto> SetRelationshipDateAsync(int userId, SetRelationshipDateRequest request);
}
