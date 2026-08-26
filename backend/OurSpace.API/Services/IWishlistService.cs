using OurSpace.API.Models.DTOs.Wishlist;

namespace OurSpace.API.Services;

public interface IWishlistService
{
    Task<WishDto> CreateAsync(int userId, CreateWishRequest request);
    Task<List<WishDto>> GetAllAsync(int userId);
    Task<WishDto> ToggleFulfilledAsync(int userId, int wishId);
    Task DeleteAsync(int userId, int wishId);
}
