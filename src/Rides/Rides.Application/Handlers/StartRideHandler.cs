using Common.Application;
using Rides.Application.Ports;
using Rides.Domain.Aggregates;
using Rides.Domain.Commands;
using Rides.Domain.ReadModels;

namespace Rides.Application.Handlers;

public class StartRideHandler : ICommandHandler<StartRideCommand>
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

    public async Task Handle(StartRideCommand command)
    {
        var riderHasActiveRide = await rideReadStore.HasActiveRideForRider(command.RiderId, command.TenantId);

        if (riderHasActiveRide)
        {
            throw new InvalidOperationException($"Rider {command.RiderId} already has an active ride.");
        }

        var ride = RideAggregate.Start(command);

        await eventStore.Append(ride);

        foreach (var domainEvent in ride.DomainEvents)
        {
            await eventPublisher.Publish(domainEvent);
        }

        await rideReadStore.Upsert(RideReadModel.Create(
            ride.Id,
            ride.TenantId,
            ride.RiderId,
            ride.DriverId,
            command.DriverName,
            ride.Fare.Amount,
            ride.Fare.Currency));

        ride.ClearDomainEvents();
    }
}
