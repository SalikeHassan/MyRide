namespace MyRide.Application.Models;

public record RideResult(
    Guid RideId,
    Guid RiderId,
    Guid DriverId,
    decimal FareAmount,
    string FareCurrency);
