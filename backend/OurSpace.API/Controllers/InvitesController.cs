using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OurSpace.API.Common;
using OurSpace.API.Models.DTOs.Auth;
using OurSpace.API.Services;

namespace OurSpace.API.Controllers;

[ApiController]
[Route("api/invites")]
[Authorize]
public class InvitesController(IInviteService inviteService) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<InviteDto>> Create()
    {
        var dto = await inviteService.CreateAsync(this.GetUserId());
        return Created(string.Empty, dto);
    }

    [HttpGet]
    public async Task<ActionResult<List<InviteDto>>> GetMine()
    {
        var invites = await inviteService.GetMineAsync(this.GetUserId());
        return Ok(invites);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Revoke(int id)
    {
        await inviteService.RevokeAsync(this.GetUserId(), id);
        return NoContent();
    }

    [HttpGet("pending")]
    public async Task<ActionResult<List<PendingAccountDto>>> GetPending()
    {
        var pending = await inviteService.GetPendingAsync(this.GetUserId());
        return Ok(pending);
    }

    [HttpPost("approve")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<IActionResult> Approve(ApproveAccountRequest request)
    {
        await inviteService.ApproveAsync(this.GetUserId(), request.Code);
        return NoContent();
    }
}
