using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Event;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public class EventService(AppDbContext db) : IEventService
{
    public async Task<EventDto> CreateAsync(int userId, CreateEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new BadRequestException("Naziv događaja je obavezan.");

        var couple = await GetCoupleOrThrow(userId);

        var ev = new Event
        {
            CoupleId = couple.Id,
            CreatedByUserId = userId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            EventDate = request.EventDate,
        };

        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var creator = await db.Users.SingleAsync(u => u.Id == userId);
        return ToDto(ev, creator.Username);
    }

    public async Task<List<EventDto>> GetUpcomingAsync(int userId)
    {
        var couple = await GetCoupleOrThrow(userId);

        return await db.Events
            .Include(e => e.CreatedByUser)
            .Where(e => e.CoupleId == couple.Id && e.EventDate >= DateTime.UtcNow.Date)
            .OrderBy(e => e.EventDate)
            .Select(e => new EventDto(e.Id, e.Title, e.Description, e.EventDate, e.CreatedByUser.Username, e.CreatedAt))
            .ToListAsync();
    }

    public async Task<EventDto> UpdateAsync(int userId, int eventId, CreateEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new BadRequestException("Naziv događaja je obavezan.");

        var couple = await GetCoupleOrThrow(userId);

        var ev = await db.Events.Include(e => e.CreatedByUser).SingleOrDefaultAsync(e => e.Id == eventId)
            ?? throw new NotFoundException("Događaj nije pronađen.");

        if (ev.CoupleId != couple.Id)
            throw new BadRequestException("Nemaš pristup ovom događaju.");

        ev.Title = request.Title.Trim();
        ev.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        ev.EventDate = request.EventDate;
        ev.ReminderSentAt = null;

        await db.SaveChangesAsync();

        return ToDto(ev, ev.CreatedByUser.Username);
    }

    public async Task DeleteAsync(int userId, int eventId)
    {
        var couple = await GetCoupleOrThrow(userId);

        var ev = await db.Events.SingleOrDefaultAsync(e => e.Id == eventId)
            ?? throw new NotFoundException("Događaj nije pronađen.");

        if (ev.CoupleId != couple.Id)
            throw new BadRequestException("Nemaš pristup ovom događaju.");

        db.Events.Remove(ev);
        await db.SaveChangesAsync();
    }

    private async Task<Couple> GetCoupleOrThrow(int userId) =>
        await db.Couples.SingleOrDefaultAsync(c => c.User1Id == userId || c.User2Id == userId)
        ?? throw new BadRequestException("Moraš biti uparen/a sa partnerom da bi dodao/la događaj.");

    private static EventDto ToDto(Event ev, string createdByUsername) =>
        new(ev.Id, ev.Title, ev.Description, ev.EventDate, createdByUsername, ev.CreatedAt);
}
