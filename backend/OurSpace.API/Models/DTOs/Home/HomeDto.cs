using OurSpace.API.Models.DTOs.Event;
using OurSpace.API.Models.DTOs.Memory;
using OurSpace.API.Models.DTOs.User;
using OurSpace.API.Models.DTOs.Wishlist;

namespace OurSpace.API.Models.DTOs.Home;

public record HomeDto(
    UserDto User,
    List<EventDto> UpcomingEvents,
    List<WishDto> RecentWishes,
    List<PhotoDto> Photos,
    List<AudioDto> Audio,
    int TotalMemories,
    List<MemoryRow> OnThisDay
);
