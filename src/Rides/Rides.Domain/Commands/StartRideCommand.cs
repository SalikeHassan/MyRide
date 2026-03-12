namespace Rides.Domain.Commands;

public class StartRideCommand
{
    public Guid RideId { get; }
    public string TenantId { get; }
    public Guid RiderId { get; }
    public Guid DriverId { get; }
    public decimal FareAmount { get; }
    public string FareCurrency { get; }
    public double PickupLat { get; }
    public double PickupLng { get; }
    public double DropoffLat { get; }
    public double DropoffLng { get; }

    public StartRideCommand(
        Guid rideId,
        string tenantId,
        Guid riderId,
        Guid driverId,
        decimal fareAmount,
        string fareCurrency,
        double pickupLat,
        double pickupLng,
        double dropoffLat,
        double dropoffLng)
    {
        RideId = rideId;
        TenantId = tenantId;
        RiderId = riderId;
        DriverId = driverId;
        FareAmount = fareAmount;
        FareCurrency = fareCurrency;
        PickupLat = pickupLat;
        PickupLng = pickupLng;
        DropoffLat = dropoffLat;
        DropoffLng = dropoffLng;
    }
}
