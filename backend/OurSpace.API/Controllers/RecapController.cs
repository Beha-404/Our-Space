using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OurSpace.API.Common;
using OurSpace.API.Models.DTOs.Recap;
using OurSpace.API.Services;

namespace OurSpace.API.Controllers;

[ApiController]
[Route("api/recap")]
[Authorize]
public class RecapController(IRecapService recapService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RecapDto>> Get([FromQuery] int? year = null)
    {
        var recap = await recapService.GetAsync(this.GetUserId(), year);
        return Ok(recap);
    }
}
