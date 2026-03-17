using Common.Domain;

namespace Rides.Domain.Events;

public class RideCompleted : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string TenantId { get; }

    public Guid RideId { get; }
    public Guid RiderId { get; }
    public Guid DriverId { get; }
    public decimal FareAmount { get; }
    public string FareCurrency { get; }

    public RideCompleted(
        string tenantId,
        Guid rideId,
        Guid riderId,
        Guid driverId,
        decimal fareAmount,
        string fareCurrency)
    {
        TenantId = tenantId;
        RideId = rideId;
        RiderId = riderId;
        DriverId = driverId;
        FareAmount = fareAmount;
        FareCurrency = fareCurrency;
    }
}
