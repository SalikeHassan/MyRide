using SharedKernel;

namespace Rides.Domain.Events;

public class RideStarted : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string TenantId { get; }

    public Guid RideId { get; }
    public Guid RiderId { get; }
    public Guid DriverId { get; }
    public decimal FareAmount { get; }
    public string FareCurrency { get; }
    public double PickupLat { get; }
    public double PickupLng { get; }
    public double DropoffLat { get; }
    public double DropoffLng { get; }

    public RideStarted(
        string tenantId,
        Guid rideId,
        Guid riderId,
        Guid driverId,
        decimal fareAmount,
        string fareCurrency,
        double pickupLat,
        double pickupLng,
        double dropoffLat,
        double dropoffLng)
    {
        TenantId = tenantId;
        RideId = rideId;
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
