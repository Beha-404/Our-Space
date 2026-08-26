using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OurSpace.API.Common;
using OurSpace.API.Models.DTOs.Wishlist;
using OurSpace.API.Services;

namespace OurSpace.API.Controllers;

[ApiController]
[Route("api/wishlist")]
[Authorize]
public class WishlistController(IWishlistService wishlistService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<WishDto>> Create(CreateWishRequest request)
    {
        var dto = await wishlistService.CreateAsync(this.GetUserId(), request);
        return Created(string.Empty, dto);
    }

    [HttpGet]
    public async Task<ActionResult<List<WishDto>>> GetAll()
    {
        var wishes = await wishlistService.GetAllAsync(this.GetUserId());
        return Ok(wishes);
    }

    [HttpPut("{id:int}/toggle")]
    public async Task<ActionResult<WishDto>> ToggleFulfilled(int id)
    {
        var dto = await wishlistService.ToggleFulfilledAsync(this.GetUserId(), id);
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await wishlistService.DeleteAsync(this.GetUserId(), id);
        return NoContent();
    }
}
