namespace MyRide.Infrastructure.Models;

public record RidesApiRequest(
    Guid RideId,
    Guid RiderId,
    Guid DriverId,
    string DriverName,
    decimal FareAmount,
    string FareCurrency,
    double PickupLat,
    double PickupLng,
    double DropoffLat,
    double DropoffLng);
