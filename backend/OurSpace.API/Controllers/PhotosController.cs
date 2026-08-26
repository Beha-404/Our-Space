using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OurSpace.API.Common;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;
using OurSpace.API.Models.DTOs.Memory;
using OurSpace.API.Services;

namespace OurSpace.API.Controllers;

[ApiController]
[Route("api/photos")]
[Authorize]
public class PhotosController(IPhotoService photoService, ILocalizer localizer) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<PhotoDto>> Upload(
        [FromForm] IFormFile file,
        [FromForm] DateOnly takenAt,
        [FromForm] string? caption)
    {
        if (file is null)
            throw new BadRequestException(localizer.T("Photo.FileRequired"));

        var dto = await photoService.UploadAsync(this.GetUserId(), file, takenAt, caption);
        return Created(string.Empty, dto);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PhotoDto>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var photos = await photoService.GetAllAsync(this.GetUserId(), page, pageSize);
        return Ok(photos);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await photoService.DeleteAsync(this.GetUserId(), id);
        return NoContent();
    }
}
