using Common.Domain;

namespace Rides.Domain.Events;

public class RideAccepted : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string TenantId { get; }

    public Guid RideId { get; }
    public Guid DriverId { get; }

    public RideAccepted(string tenantId, Guid rideId, Guid driverId)
    {
        TenantId = tenantId;
        RideId = rideId;
        DriverId = driverId;
    }
}
