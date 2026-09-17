using System.Globalization;
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
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<PhotoDto>> Upload(
        [FromForm] IFormFile file,
        [FromForm] DateOnly takenAt,
        [FromForm] string? caption,
        [FromForm] string? latitude,
        [FromForm] string? longitude,
        [FromForm] string? locationName)
    {
        if (file is null)
            throw new BadRequestException(localizer.T("Photo.FileRequired"));

        var location = ParseLocation(latitude, longitude, locationName);

        var dto = await photoService.UploadAsync(this.GetUserId(), file, takenAt, caption, location);
        return Created(string.Empty, dto);
    }

    private ManualLocation? ParseLocation(string? latitude, string? longitude, string? locationName)
    {
        if (string.IsNullOrWhiteSpace(latitude) && string.IsNullOrWhiteSpace(longitude))
            return null;

        if (!double.TryParse(latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
            || !double.TryParse(longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
        {
            throw new BadRequestException(localizer.T("Photo.InvalidLocation"));
        }

        return new ManualLocation(lat, lon, locationName);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PhotoDto>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var photos = await photoService.GetAllAsync(this.GetUserId(), page, pageSize);
        return Ok(photos);
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetCount()
    {
        var count = await photoService.GetCountAsync(this.GetUserId());
        return Ok(count);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await photoService.DeleteAsync(this.GetUserId(), id);
        return NoContent();
    }
}
