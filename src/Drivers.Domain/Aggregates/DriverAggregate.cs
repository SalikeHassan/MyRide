using Common.Domain;
using Drivers.Domain.Commands;
using Drivers.Domain.Events;

namespace Drivers.Domain.Aggregates;

public class DriverAggregate : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public bool IsAssigned { get; private set; }

    private DriverAggregate() { }

    public static DriverAggregate Load(IEnumerable<IDomainEvent> events)
    {
        var driver = new DriverAggregate();
        driver.Rehydrate(events);
        return driver;
    }

    public static DriverAggregate Onboard(OnboardDriverCommand command)
    {
        var driver = new DriverAggregate();

        driver.RaiseEvent(new DriverOnboarded(
            command.DriverId,
            command.TenantId,
            command.Name,
            command.Phone,
            DateTime.UtcNow));

        return driver;
    }

    public void Assign(Guid rideId)
    {
        if (IsAssigned)
        {
            throw new InvalidOperationException($"Driver {Id} already has an active ride.");
        }

        RaiseEvent(new DriverAssigned(Id, rideId, TenantId, DateTime.UtcNow));
    }

    public void Free(Guid rideId)
    {
        if (!IsAssigned)
        {
            return;
        }

        RaiseEvent(new DriverFreed(Id, rideId, TenantId, DateTime.UtcNow));
    }

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case DriverOnboarded e:
                Apply(e);
                break;
            case DriverAssigned e:
                Apply(e);
                break;
            case DriverFreed e:
                Apply(e);
                break;
            default:
                throw new InvalidOperationException($"Unknown event type: {domainEvent.GetType().Name}");
        }
    }

    private void Apply(DriverOnboarded e)
    {
        Id = e.DriverId;
        TenantId = e.TenantId;
        Name = e.Name;
        Phone = e.Phone;
        IsAssigned = false;
    }

    private void Apply(DriverAssigned e)
    {
        IsAssigned = true;
    }

    private void Apply(DriverFreed e)
    {
        IsAssigned = false;
    }
}
