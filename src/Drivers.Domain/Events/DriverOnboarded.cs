using Common.Domain;

namespace Drivers.Domain.Events;

public record DriverOnboarded(
    Guid DriverId,
    string TenantId,
    string Name,
    string Phone,
    DateTime OnboardedAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
