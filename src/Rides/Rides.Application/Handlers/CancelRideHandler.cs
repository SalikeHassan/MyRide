using Common.Application;
using Rides.Application.Ports;
using Rides.Domain.Commands;

namespace Rides.Application.Handlers;

public class CancelRideHandler : ICommandHandler<CancelRideCommand>
{
    private readonly IRideEventStore eventStore;
    private readonly IRideEventPublisher eventPublisher;
    private readonly IRideReadStore rideReadStore;

    public CancelRideHandler(
        IRideEventStore eventStore,
        IRideEventPublisher eventPublisher,
        IRideReadStore rideReadStore)
    {
        this.eventStore = eventStore;
        this.eventPublisher = eventPublisher;
        this.rideReadStore = rideReadStore;
    }

    public async Task Handle(CancelRideCommand command)
    {
        var ride = await eventStore.Load(command.RideId, command.TenantId);

        ride.Cancel(command.Reason);

        await eventStore.Append(ride);

        foreach (var domainEvent in ride.DomainEvents)
        {
            await eventPublisher.Publish(domainEvent);
        }

        var readModel = await rideReadStore.GetById(ride.Id, ride.TenantId);
        readModel!.Cancel();
        await rideReadStore.Upsert(readModel);

        ride.ClearDomainEvents();
    }
}
