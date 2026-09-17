using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace OurSpace.API.Services;

public static class PhotoLocation
{
    private const int StoredPrecision = 4;

    public static (double Latitude, double Longitude)? Read(Image image)
    {
        var exif = image.Metadata.ExifProfile;
        if (exif is null)
            return null;

        if (!exif.TryGetValue(ExifTag.GPSLatitude, out var latitudeValue)
            || !exif.TryGetValue(ExifTag.GPSLongitude, out var longitudeValue)
            || latitudeValue.Value is not { Length: 3 } latitudeParts
            || longitudeValue.Value is not { Length: 3 } longitudeParts)
        {
            return null;
        }

        var latitude = ToDegrees(latitudeParts);
        var longitude = ToDegrees(longitudeParts);

        if (exif.TryGetValue(ExifTag.GPSLatitudeRef, out var latitudeRef) && latitudeRef.Value == "S")
            latitude = -latitude;

        if (exif.TryGetValue(ExifTag.GPSLongitudeRef, out var longitudeRef) && longitudeRef.Value == "W")
            longitude = -longitude;

        if (double.IsNaN(latitude) || double.IsNaN(longitude))
            return null;

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            return null;

        if (latitude == 0 && longitude == 0)
            return null;

        return (Math.Round(latitude, StoredPrecision), Math.Round(longitude, StoredPrecision));
    }

    private static double ToDegrees(Rational[] parts) =>
        parts[0].ToDouble() + parts[1].ToDouble() / 60 + parts[2].ToDouble() / 3600;
}
