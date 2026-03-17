using Common.Domain;

namespace Drivers.Domain.Events;

public record DriverFreed(
    Guid DriverId,
    Guid RideId,
    string TenantId,
    DateTime FreedAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
