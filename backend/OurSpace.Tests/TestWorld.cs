using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OurSpace.API.Data;
using OurSpace.API.Models.Entities;
using OurSpace.API.Services;

namespace OurSpace.Tests;

public record SeededWorld(
    int EventId,
    int PhotoId,
    int AudioId,
    int WishId,
    int NotificationId,
    string AnaToken,
    string MarkoToken,
    string LejlaToken);

public static class TestWorld
{
    public const string EventTitle = "Godišnjica";
    public const string PhotoCaption = "Piknik";
    public const string AudioCaption = "Laku noć";
    public const string WishTitle = "Put u Mostar";

    public static async Task<SeededWorld> SeedAsync(OurSpaceFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var ana = NewUser("ana");
        var marko = NewUser("marko");
        var lejla = NewUser("lejla");
        var emir = NewUser("emir");

        db.Users.AddRange(ana, marko, lejla, emir);
        await db.SaveChangesAsync();

        var coupleA = new Couple { User1Id = ana.Id, User2Id = marko.Id };
        var coupleB = new Couple { User1Id = lejla.Id, User2Id = emir.Id };

        db.Couples.AddRange(coupleA, coupleB);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var ev = new Event
        {
            CoupleId = coupleA.Id,
            CreatedByUserId = ana.Id,
            Title = EventTitle,
            EventDate = DateTime.UtcNow.AddDays(10),
        };

        var photo = new Photo
        {
            CoupleId = coupleA.Id,
            UploadedByUserId = ana.Id,
            FilePath = "/uploads/photos/seed.jpg",
            ThumbnailPath = "/uploads/photos/seed-thumb.jpg",
            Caption = PhotoCaption,
            TakenAt = today,
        };

        var audio = new AudioMessage
        {
            CoupleId = coupleA.Id,
            UploadedByUserId = ana.Id,
            FilePath = "/uploads/audio/seed.webm",
            Caption = AudioCaption,
            RecordedAt = today,
        };

        var wish = new WishlistItem
        {
            CoupleId = coupleA.Id,
            CreatedByUserId = ana.Id,
            Title = WishTitle,
        };

        db.Events.Add(ev);
        db.Photos.Add(photo);
        db.AudioMessages.Add(audio);
        db.WishlistItems.Add(wish);
        await db.SaveChangesAsync();

        var notification = new Notification
        {
            RecipientUserId = marko.Id,
            ActorUserId = ana.Id,
            Type = NotificationType.EventCreated,
            EntityType = "event",
            EntityId = ev.Id,
            EntityTitle = EventTitle,
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        return new SeededWorld(
            ev.Id,
            photo.Id,
            audio.Id,
            wish.Id,
            notification.Id,
            tokens.GenerateAccessToken(ana),
            tokens.GenerateAccessToken(marko),
            tokens.GenerateAccessToken(lejla));
    }

    public static HttpClient ClientFor(OurSpaceFactory factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<Event?> FindEventAsync(OurSpaceFactory factory, int eventId) =>
        await QueryAsync(factory, db => db.Events.SingleOrDefaultAsync(e => e.Id == eventId));

    public static async Task<Photo?> FindPhotoAsync(OurSpaceFactory factory, int photoId) =>
        await QueryAsync(factory, db => db.Photos.SingleOrDefaultAsync(p => p.Id == photoId));

    public static async Task<AudioMessage?> FindAudioAsync(OurSpaceFactory factory, int audioId) =>
        await QueryAsync(factory, db => db.AudioMessages.SingleOrDefaultAsync(a => a.Id == audioId));

    public static async Task<WishlistItem?> FindWishAsync(OurSpaceFactory factory, int wishId) =>
        await QueryAsync(factory, db => db.WishlistItems.SingleOrDefaultAsync(w => w.Id == wishId));

    public static async Task<Notification?> FindNotificationAsync(OurSpaceFactory factory, int notificationId) =>
        await QueryAsync(factory, db => db.Notifications.SingleOrDefaultAsync(n => n.Id == notificationId));

    private static async Task<T> QueryAsync<T>(OurSpaceFactory factory, Func<AppDbContext, Task<T>> query)
    {
        using var scope = factory.Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private static User NewUser(string name) => new()
    {
        Username = name,
        Email = $"{name}@test.local",
        PasswordHash = "not-a-real-hash",
    };
}
