using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OurSpace.API.Common;
using OurSpace.API.Models.DTOs.Memory;
using OurSpace.API.Services;

namespace OurSpace.API.Controllers;

[ApiController]
[Route("api/memories")]
[Authorize]
public class MemoriesController(IMemoryFeedService feedService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MemoryFeedDto>> GetFeed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        [FromQuery] string sort = "newest",
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] string? type = null,
        [FromQuery] string? search = null)
    {
        var feed = await feedService.GetPageAsync(
            this.GetUserId(),
            new MemoryFeedQuery(page, pageSize, sort, year, month, type, search));

        return Ok(feed);
    }

    [HttpGet("map")]
    public async Task<ActionResult<MemoryMapDto>> GetMap()
    {
        var map = await feedService.GetMapAsync(this.GetUserId());
        return Ok(map);
    }
}
