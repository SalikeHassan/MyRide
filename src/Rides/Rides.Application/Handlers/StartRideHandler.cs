using Rides.Application.Ports;
using Rides.Domain.Aggregates;
using Rides.Domain.Commands;

namespace Rides.Application.Handlers;

public class StartRideHandler
{
    private readonly IRideEventStore eventStore;
    private readonly IRideEventPublisher eventPublisher;
    private readonly IRideReadStore rideReadStore;

    public StartRideHandler(
        IRideEventStore eventStore,
        IRideEventPublisher eventPublisher,
        IRideReadStore rideReadStore)
    {
        this.eventStore = eventStore;
        this.eventPublisher = eventPublisher;
        this.rideReadStore = rideReadStore;
    }

    public async Task HandleAsync(StartRideCommand command)
    {
        var riderHasActiveRide = await rideReadStore.HasActiveRideForRiderAsync(command.RiderId, command.TenantId);

        if (riderHasActiveRide)
        {
            throw new InvalidOperationException($"Rider {command.RiderId} already has an active ride.");
        }

        var driverHasActiveRide = await rideReadStore.HasActiveRideForDriverAsync(command.DriverId, command.TenantId);

        if (driverHasActiveRide)
        {
            throw new InvalidOperationException($"Driver {command.DriverId} already has an active ride.");
        }

        var ride = RideAggregate.Start(command);

        await eventStore.AppendAsync(ride);

        foreach (var domainEvent in ride.DomainEvents)
        {
            await eventPublisher.PublishAsync(domainEvent);
        }

        await rideReadStore.UpsertAsync(new RideReadModel
        {
            RideId = ride.Id,
            TenantId = ride.TenantId,
            RiderId = ride.RiderId,
            DriverId = ride.DriverId,
            Status = ride.Status,
            LastUpdatedOn = DateTime.UtcNow
        });

        ride.ClearDomainEvents();
    }
}
