namespace OurSpace.API.Models.DTOs.Home;

public record PartnerActivityDto(
    int Photos,
    int VoiceLetters,
    int Wishes,
    int Events,
    int Capsules,
    List<PartnerPhotoDto> RecentPhotos
);

public record PartnerPhotoDto(
    int Id,
    string ThumbnailUrl,
    string? Caption,
    DateOnly TakenAt
);
