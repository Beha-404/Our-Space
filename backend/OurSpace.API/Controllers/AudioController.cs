using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OurSpace.API.Common;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Models.DTOs.Memory;
using OurSpace.API.Services;

namespace OurSpace.API.Controllers;

[ApiController]
[Route("api/audio")]
[Authorize]
public class AudioController(IAudioService audioService) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<AudioDto>> Upload(
        [FromForm] IFormFile file,
        [FromForm] DateOnly recordedAt,
        [FromForm] string? caption)
    {
        if (file is null)
            throw new BadRequestException("Fajl je obavezan.");

        var dto = await audioService.UploadAsync(this.GetUserId(), file, recordedAt, caption);
        return Created(string.Empty, dto);
    }

    [HttpGet]
    public async Task<ActionResult<List<AudioDto>>> GetAll()
    {
        var audio = await audioService.GetAllAsync(this.GetUserId());
        return Ok(audio);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await audioService.DeleteAsync(this.GetUserId(), id);
        return NoContent();
    }
}
