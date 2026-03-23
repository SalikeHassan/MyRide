using Common.Application;
using Rides.Application.Ports;
using Rides.Domain.Commands;

namespace Rides.Application.Handlers;

public class CancelRideHandler : ICommandHandler<CancelRideCommand>
{
    private readonly IRideEventStore eventStore;
    private readonly IRideReadStore rideReadStore;

    public CancelRideHandler(
        IRideEventStore eventStore,
        IRideReadStore rideReadStore)
    {
        this.eventStore = eventStore;
        this.rideReadStore = rideReadStore;
    }

    public async Task Handle(CancelRideCommand command)
    {
        var ride = await eventStore.Load(command.RideId, command.TenantId);

        ride.Cancel(command.Reason);

        await eventStore.Append(ride);

        var readModel = await rideReadStore.GetById(ride.Id, ride.TenantId);
        readModel!.Cancel();
        await rideReadStore.Upsert(readModel);
    }
}
