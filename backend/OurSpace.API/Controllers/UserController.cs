using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OurSpace.API.Common;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;
using OurSpace.API.Models.DTOs.User;
using OurSpace.API.Services;

namespace OurSpace.API.Controllers;

[ApiController]
[Route("api/user")]
[Authorize]
public class UserController(IUserService userService, ILocalizer localizer) : ControllerBase
{
    [HttpGet("current")]
    public async Task<ActionResult<UserDto>> GetCurrent()
    {
        var dto = await userService.GetCurrentAsync(this.GetUserId());
        return Ok(dto);
    }

    [HttpPut]
    public async Task<ActionResult<UserDto>> Update(UpdateUserRequest request)
    {
        var dto = await userService.UpdateAsync(this.GetUserId(), request);
        return Ok(dto);
    }

    [HttpPost("profile-picture")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<UserDto>> UpdateProfilePicture([FromForm] IFormFile file)
    {
        if (file is null)
            throw new BadRequestException(localizer.T("User.FileRequired"));

        var dto = await userService.UpdateProfilePictureAsync(this.GetUserId(), file);
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (id != this.GetUserId())
            return Forbid();

        await userService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("pairing-code")]
    public async Task<ActionResult<PairingCodeResponse>> GeneratePairingCode()
    {
        var response = await userService.GeneratePairingCodeAsync(this.GetUserId());
        return Ok(response);
    }

    [HttpPost("pair")]
    public async Task<ActionResult<UserDto>> Pair(PairRequest request)
    {
        var dto = await userService.PairAsync(this.GetUserId(), request);
        return Ok(dto);
    }

    [HttpPost("unpair")]
    public async Task<ActionResult<UserDto>> Unpair()
    {
        var dto = await userService.UnpairAsync(this.GetUserId());
        return Ok(dto);
    }

    [HttpPut("relationship-date")]
    public async Task<ActionResult<UserDto>> SetRelationshipDate(SetRelationshipDateRequest request)
    {
        var dto = await userService.SetRelationshipDateAsync(this.GetUserId(), request);
        return Ok(dto);
    }

    [HttpPut("language")]
    public async Task<IActionResult> UpdateLanguage(UpdateLanguageRequest request)
    {
        await userService.UpdateLanguageAsync(this.GetUserId(), request.Language);
        return NoContent();
    }

    [HttpPost("email-change")]
    public async Task<IActionResult> RequestEmailChange(RequestEmailChangeRequest request)
    {
        await userService.RequestEmailChangeAsync(this.GetUserId(), request.NewEmail);
        return NoContent();
    }

    [HttpPost("email-change/confirm")]
    public async Task<ActionResult<UserDto>> ConfirmEmailChange(ConfirmEmailChangeRequest request)
    {
        var dto = await userService.ConfirmEmailChangeAsync(this.GetUserId(), request.Code);
        return Ok(dto);
    }

    [HttpDelete("email-change")]
    public async Task<ActionResult<UserDto>> CancelEmailChange()
    {
        var dto = await userService.CancelEmailChangeAsync(this.GetUserId());
        return Ok(dto);
    }
}
