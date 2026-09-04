using Microsoft.EntityFrameworkCore;
using OurSpace.API.Common.Exceptions;
using OurSpace.API.Common.Localization;
using OurSpace.API.Data;
using OurSpace.API.Models.DTOs.Event;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Services;

public class EventService(AppDbContext db, IEmailQueue emailQueue, ILocalizer localizer, ICoupleContext coupleContext) : IEventService
{
    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 2000;

    public async Task<EventDto> CreateAsync(int userId, CreateEventRequest request)
    {
        ValidateTitleAndDescription(request);

        var couple = await GetCoupleWithUsersOrThrow(userId);

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

        var creator = ActingUser(couple, userId);
        await NotifyPartnerAsync(couple, userId, creator.Username, ev.Title, EventChangeType.Created);
        return ToDto(ev, creator.Username);
    }

    public async Task<List<EventDto>> GetUpcomingAsync(int userId, bool includePast = false)
    {
        var coupleId = await coupleContext.GetCoupleIdOrThrow(userId, "Event.NeedPartner");
        var today = DateTime.UtcNow.Date;

        var query = db.Events.Where(e => e.CoupleId == coupleId);

        if (!includePast)
            query = query.Where(e => e.EventDate >= today);

        return await query
            .OrderBy(e => e.EventDate < today)
            .ThenBy(e => e.EventDate >= today ? e.EventDate : DateTime.MaxValue)
            .ThenByDescending(e => e.EventDate)
            .Select(e => new EventDto(e.Id, e.Title, e.Description, e.EventDate, e.CreatedByUser.Username, e.CreatedAt))
            .ToListAsync();
    }

    public async Task<EventDto> UpdateAsync(int userId, int eventId, CreateEventRequest request)
    {
        ValidateTitleAndDescription(request);

        var couple = await GetCoupleWithUsersOrThrow(userId);

        var ev = await db.Events.Include(e => e.CreatedByUser).SingleOrDefaultAsync(e => e.Id == eventId)
            ?? throw new NotFoundException(localizer.T("Event.NotFound"));

        if (ev.CoupleId != couple.Id)
            throw new NotFoundException(localizer.T("Event.NotFound"));

        ev.Title = request.Title.Trim();
        ev.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        ev.EventDate = request.EventDate;
        ev.ReminderSentAt = null;

        await db.SaveChangesAsync();

        await NotifyPartnerAsync(couple, userId, ActingUser(couple, userId).Username, ev.Title, EventChangeType.Updated);

        return ToDto(ev, ev.CreatedByUser.Username);
    }

    public async Task DeleteAsync(int userId, int eventId)
    {
        var couple = await GetCoupleWithUsersOrThrow(userId);

        var ev = await db.Events.SingleOrDefaultAsync(e => e.Id == eventId)
            ?? throw new NotFoundException(localizer.T("Event.NotFound"));

        if (ev.CoupleId != couple.Id)
            throw new NotFoundException(localizer.T("Event.NotFound"));

        var title = ev.Title;
        db.Events.Remove(ev);
        await db.SaveChangesAsync();

        await NotifyPartnerAsync(couple, userId, ActingUser(couple, userId).Username, title, EventChangeType.Deleted);
    }

    private void ValidateTitleAndDescription(CreateEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new BadRequestException(localizer.T("Event.TitleRequired"));

        if (request.Title.Trim().Length > MaxTitleLength)
            throw new BadRequestException(localizer.T("Event.TitleTooLong"));

        if (request.Description?.Trim().Length > MaxDescriptionLength)
            throw new BadRequestException(localizer.T("Event.DescriptionTooLong"));
    }

    private async Task<Couple> GetCoupleWithUsersOrThrow(int userId) =>
        await db.Couples.Include(c => c.User1).Include(c => c.User2)
            .SingleOrDefaultAsync(c => c.User1Id == userId || c.User2Id == userId)
        ?? throw new BadRequestException(localizer.T("Event.NeedPartner"));

    private static User ActingUser(Couple couple, int userId) =>
        couple.User1Id == userId ? couple.User1 : couple.User2;

    private async Task NotifyPartnerAsync(Couple couple, int actingUserId, string actorUsername, string eventTitle, EventChangeType change)
    {
        var (subjectKey, bodyKey) = change switch
        {
            EventChangeType.Created => ("Email.EventCreated.Subject", "Email.EventCreated.Body"),
            EventChangeType.Updated => ("Email.EventUpdated.Subject", "Email.EventUpdated.Body"),
            EventChangeType.Deleted => ("Email.EventDeleted.Subject", "Email.EventDeleted.Body"),
            _ => throw new ArgumentOutOfRangeException(nameof(change)),
        };

        var recipients = new[] { couple.User1, couple.User2 }.Where(u => u.Id != actingUserId);
        foreach (var user in recipients)
        {
            var subject = localizer.For(subjectKey, user.PreferredLanguage, eventTitle);
            var body = localizer.For(bodyKey, user.PreferredLanguage, actorUsername, eventTitle);
            await emailQueue.EnqueueAsync(user.Email, subject, body);
        }
    }

    private enum EventChangeType { Created, Updated, Deleted }

    private static EventDto ToDto(Event ev, string createdByUsername) =>
        new(ev.Id, ev.Title, ev.Description, ev.EventDate, createdByUsername, ev.CreatedAt);
}
