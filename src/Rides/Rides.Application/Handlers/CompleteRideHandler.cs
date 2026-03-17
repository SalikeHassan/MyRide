using Common.Application;
using Rides.Application.Ports;
using Rides.Domain.Commands;

namespace Rides.Application.Handlers;

public class CompleteRideHandler : ICommandHandler<CompleteRideCommand>
{
    private readonly IRideEventStore eventStore;
    private readonly IRideEventPublisher eventPublisher;
    private readonly IRideReadStore rideReadStore;

    public CompleteRideHandler(
        IRideEventStore eventStore,
        IRideEventPublisher eventPublisher,
        IRideReadStore rideReadStore)
    {
        this.eventStore = eventStore;
        this.eventPublisher = eventPublisher;
        this.rideReadStore = rideReadStore;
    }

    public async Task Handle(CompleteRideCommand command)
    {
        var ride = await eventStore.Load(command.RideId, command.TenantId);

        ride.Complete();

        await eventStore.Append(ride);

        foreach (var domainEvent in ride.DomainEvents)
        {
            await eventPublisher.Publish(domainEvent);
        }

        var readModel = await rideReadStore.GetById(ride.Id, ride.TenantId);
        readModel!.Complete();
        await rideReadStore.Upsert(readModel);

        ride.ClearDomainEvents();
    }
}
