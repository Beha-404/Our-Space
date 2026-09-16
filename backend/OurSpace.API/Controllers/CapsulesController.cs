using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OurSpace.API.Common;
using OurSpace.API.Models.DTOs.Capsule;
using OurSpace.API.Services;

namespace OurSpace.API.Controllers;

[ApiController]
[Route("api/capsules")]
[Authorize]
public class CapsulesController(ITimeCapsuleService capsuleService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CapsuleDto>> Create(CreateCapsuleRequest request)
    {
        var dto = await capsuleService.CreateAsync(this.GetUserId(), request);
        return Created(string.Empty, dto);
    }

    [HttpGet]
    public async Task<ActionResult<List<CapsuleDto>>> GetAll()
    {
        var capsules = await capsuleService.GetAllAsync(this.GetUserId());
        return Ok(capsules);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CapsuleDto>> Get(int id)
    {
        var dto = await capsuleService.GetAsync(this.GetUserId(), id);
        return Ok(dto);
    }

    [HttpPost("{id:int}/open")]
    public async Task<ActionResult<CapsuleDto>> Open(int id)
    {
        var dto = await capsuleService.OpenAsync(this.GetUserId(), id);
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await capsuleService.DeleteAsync(this.GetUserId(), id);
        return NoContent();
    }
}
