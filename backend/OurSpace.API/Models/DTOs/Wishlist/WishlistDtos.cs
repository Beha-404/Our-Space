namespace OurSpace.API.Models.DTOs.Wishlist;

public record CreateWishRequest(string Title);

public record WishDto(
    int Id,
    string Title,
    bool IsFulfilled,
    DateTime? FulfilledAt,
    string CreatedByUsername,
    DateTime CreatedAt
);
