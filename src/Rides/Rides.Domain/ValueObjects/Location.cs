using Common.Domain;

namespace Rides.Domain.ValueObjects;

public class Location : ValueObject
{
    public double Latitude { get; }
    public double Longitude { get; }

    public Location(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
        {
            throw new ArgumentException("Latitude must be between -90 and 90.", nameof(latitude));
        }

        if (longitude < -180 || longitude > 180)
        {
            throw new ArgumentException("Longitude must be between -180 and 180.", nameof(longitude));
        }

        Latitude = latitude;
        Longitude = longitude;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }
}
