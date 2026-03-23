using Common.Application;
using Rides.Application.Ports;
using Rides.Domain.Commands;

namespace Rides.Application.Handlers;

public class AcceptRideHandler : ICommandHandler<AcceptRideCommand>
{
    private readonly IRideEventStore eventStore;
    private readonly IRideReadStore rideReadStore;

    public AcceptRideHandler(
        IRideEventStore eventStore,
        IRideReadStore rideReadStore)
    {
        this.eventStore = eventStore;
        this.rideReadStore = rideReadStore;
    }

    public async Task Handle(AcceptRideCommand command)
    {
        var ride = await eventStore.Load(command.RideId, command.TenantId);

        ride.Accept();

        await eventStore.Append(ride);

        var readModel = await rideReadStore.GetById(ride.Id, ride.TenantId);
        readModel!.Accept();
        await rideReadStore.Upsert(readModel);
    }
}
