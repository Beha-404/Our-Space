using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OurSpace.API.Common;
using OurSpace.API.Models.DTOs.Event;
using OurSpace.API.Services;

namespace OurSpace.API.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventsController(IEventService eventService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<EventDto>> Create(CreateEventRequest request)
    {
        var dto = await eventService.CreateAsync(this.GetUserId(), request);
        return Created(string.Empty, dto);
    }

    [HttpGet]
    public async Task<ActionResult<List<EventDto>>> GetUpcoming([FromQuery] bool includePast = false)
    {
        var events = await eventService.GetUpcomingAsync(this.GetUserId(), includePast);
        return Ok(events);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EventDto>> Update(int id, CreateEventRequest request)
    {
        var dto = await eventService.UpdateAsync(this.GetUserId(), id, request);
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await eventService.DeleteAsync(this.GetUserId(), id);
        return NoContent();
    }
}
