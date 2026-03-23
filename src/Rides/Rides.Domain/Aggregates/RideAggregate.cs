using Common.Domain;
using Rides.Domain.Commands;
using Rides.Domain.Events;
using Rides.Domain.ValueObjects;


namespace Rides.Domain.Aggregates;

public class RideAggregate : AggregateRoot
{
    public Guid RiderId { get; private set; }
    public Guid DriverId { get; private set; }
    public Fare Fare { get; private set; } = null!;
    public Location PickupLocation { get; private set; } = null!;
    public Location DropoffLocation { get; private set; } = null!;
    public RideStatus Status { get; private set; }

    private RideAggregate() { }

    public static RideAggregate Load(IEnumerable<IDomainEvent> events)
    {
        var ride = new RideAggregate();
        ride.Rehydrate(events);
        return ride;
    }

    public static RideAggregate Start(RequestRideCommand command)
    {
        if (command.RiderId == Guid.Empty)
        {
            throw new ArgumentException("RiderId must not be empty.", nameof(command.RiderId));
        }

        if (command.DriverId == Guid.Empty)
        {
            throw new ArgumentException("DriverId must not be empty.", nameof(command.DriverId));
        }

        var ride = new RideAggregate();

        var fareValueObject = new Fare(command.FareAmount, command.FareCurrency);
        var pickup = new Location(command.PickupLat, command.PickupLng);
        var dropoff = new Location(command.DropoffLat, command.DropoffLng);

        ride.RaiseEvent(new RideRequested(
            command.TenantId,
            command.RideId,
            command.RiderId,
            command.DriverId,
            fareValueObject.Amount,
            fareValueObject.Currency,
            pickup.Latitude,
            pickup.Longitude,
            dropoff.Latitude,
            dropoff.Longitude));

        return ride;
    }

    public void Accept()
    {
        if (Status != RideStatus.Requested)
        {
            throw new InvalidOperationException("Only a requested ride can be accepted by the driver.");
        }

        RaiseEvent(new RideAccepted(TenantId, Id, DriverId));
    }

    public void Complete()
    {
        if (Status == RideStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot complete a cancelled ride.");
        }

        if (Status == RideStatus.Completed)
        {
            throw new InvalidOperationException("Ride is already completed.");
        }

        if (Status != RideStatus.InProgress)
        {
            throw new InvalidOperationException("Only an in-progress ride can be completed.");
        }

        RaiseEvent(new RideCompleted(
            TenantId,
            Id,
            RiderId,
            DriverId,
            Fare.Amount,
            Fare.Currency));
    }

    public void Cancel(string reason)
    {
        if (Status == RideStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel a completed ride.");
        }

        if (Status == RideStatus.Cancelled)
        {
            throw new InvalidOperationException("Ride is already cancelled.");
        }

        RaiseEvent(new RideCancelled(
            TenantId,
            Id,
            RiderId,
            reason));
    }

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case RideRequested e:
                Apply(e);
                break;
            case RideAccepted e:
                Apply(e);
                break;
            case RideCompleted e:
                Apply(e);
                break;
            case RideCancelled e:
                Apply(e);
                break;
            default:
                throw new InvalidOperationException($"Unknown event type: {domainEvent.GetType().Name}");
        }
    }

    private void Apply(RideRequested e)
    {
        Id = e.RideId;
        TenantId = e.TenantId;
        RiderId = e.RiderId;
        DriverId = e.DriverId;
        Fare = new Fare(e.FareAmount, e.FareCurrency);
        PickupLocation = new Location(e.PickupLat, e.PickupLng);
        DropoffLocation = new Location(e.DropoffLat, e.DropoffLng);
        Status = RideStatus.Requested;
    }

    private void Apply(RideAccepted e)
    {
        Status = RideStatus.InProgress;
    }

    private void Apply(RideCompleted e)
    {
        Status = RideStatus.Completed;
    }

    private void Apply(RideCancelled e)
    {
        Status = RideStatus.Cancelled;
    }
}
