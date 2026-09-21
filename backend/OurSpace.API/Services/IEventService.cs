using OurSpace.API.Models.DTOs.Event;

namespace OurSpace.API.Services;

public interface IEventService
{
    Task<EventDto> CreateAsync(int userId, CreateEventRequest request);
    Task<EventDto> UpdateAsync(int userId, int eventId, CreateEventRequest request);
    Task<List<EventDto>> GetUpcomingAsync(int userId, bool includePast = false);
    Task<EventDto> CancelAsync(int userId, int eventId);
    Task<EventDto> RestoreAsync(int userId, int eventId);
}
