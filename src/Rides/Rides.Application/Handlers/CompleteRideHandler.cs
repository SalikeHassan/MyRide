using Common.Application;
using Rides.Application.Ports;
using Rides.Domain.Commands;

namespace Rides.Application.Handlers;

public class CompleteRideHandler : ICommandHandler<CompleteRideCommand>
{
    private readonly IRideEventStore eventStore;
    private readonly IRideReadStore rideReadStore;

    public CompleteRideHandler(
        IRideEventStore eventStore,
        IRideReadStore rideReadStore)
    {
        this.eventStore = eventStore;
        this.rideReadStore = rideReadStore;
    }

    public async Task Handle(CompleteRideCommand command)
    {
        var ride = await eventStore.Load(command.RideId, command.TenantId);

        ride.Complete();

        await eventStore.Append(ride);

        var readModel = await rideReadStore.GetById(ride.Id, ride.TenantId);
        readModel!.Complete();
        await rideReadStore.Upsert(readModel);
    }
}
