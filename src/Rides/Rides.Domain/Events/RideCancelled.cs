using Common.Domain;

namespace Rides.Domain.Events;

public class RideCancelled : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string TenantId { get; }

    public Guid RideId { get; }
    public Guid RiderId { get; }
    public string Reason { get; }

    public RideCancelled(
        string tenantId,
        Guid rideId,
        Guid riderId,
        string reason)
    {
        TenantId = tenantId;
        RideId = rideId;
        RiderId = riderId;
        Reason = reason;
    }
}
