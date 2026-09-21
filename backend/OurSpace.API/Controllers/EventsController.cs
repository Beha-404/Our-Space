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

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<EventDto>> Cancel(int id) =>
        Ok(await eventService.CancelAsync(this.GetUserId(), id));

    [HttpPost("{id:int}/restore")]
    public async Task<ActionResult<EventDto>> Restore(int id) =>
        Ok(await eventService.RestoreAsync(this.GetUserId(), id));
}
