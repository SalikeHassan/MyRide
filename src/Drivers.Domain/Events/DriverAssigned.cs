using Common.Domain;

namespace Drivers.Domain.Events;

public record DriverAssigned(
    Guid DriverId,
    Guid RideId,
    string TenantId,
    DateTime AssignedAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
