using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OurSpace.API.Common;
using OurSpace.API.Models.DTOs.Event;
using OurSpace.API.Models.DTOs.Home;
using OurSpace.API.Models.DTOs.Memory;
using OurSpace.API.Models.DTOs.Wishlist;
using OurSpace.API.Services;

namespace OurSpace.API.Controllers;

[ApiController]
[Route("api/home")]
[Authorize]
public class HomeController(
    IUserService userService,
    IEventService eventService,
    IWishlistService wishlistService,
    IPhotoService photoService,
    IAudioService audioService,
    IMemoryFeedService memoryFeedService,
    IRecapService recapService,
    IPartnerActivityService partnerActivityService) : ControllerBase
{
    private const int PreviewSize = 10;
    private const int EventPreviewSize = 3;
    private const int WishPreviewSize = 3;

    [HttpGet]
    public async Task<ActionResult<HomeDto>> Get()
    {
        var userId = this.GetUserId();
        var user = await userService.GetCurrentAsync(userId);

        if (user.Partner is null)
            return Ok(new HomeDto(user, [], [], [], [], 0, [], null));

        var events = await eventService.GetUpcomingAsync(userId);
        var wishes = await wishlistService.GetAllAsync(userId);
        var photos = await photoService.GetAllAsync(userId, 1, PreviewSize);
        var audio = await audioService.GetAllAsync(userId, 1, PreviewSize);
        var totalMemories = await photoService.GetCountAsync(userId) + await audioService.GetCountAsync(userId);
        var onThisDay = await memoryFeedService.GetOnThisDayAsync(userId);
        var yearTeaser = await recapService.GetTeaserAsync(userId);

        return Ok(new HomeDto(
            user,
            events.Take(EventPreviewSize).ToList(),
            wishes.Take(WishPreviewSize).ToList(),
            photos.Items,
            audio.Items,
            totalMemories,
            onThisDay,
            yearTeaser));
    }

    [HttpGet("partner")]
    public async Task<ActionResult<PartnerActivityDto>> GetPartner()
    {
        return Ok(await partnerActivityService.GetAsync(this.GetUserId()));
    }
}
