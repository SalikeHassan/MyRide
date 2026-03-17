namespace MyRide.Infrastructure.Models;

public record ActiveRideResponse(
    Guid RideId,
    string TenantId,
    Guid RiderId,
    Guid DriverId,
    string DriverName,
    string Status,
    decimal FareAmount,
    string FareCurrency,
    DateTime LastUpdatedOn);
