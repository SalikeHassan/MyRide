using Common.Application;
using Rides.Application.Ports;
using Rides.Domain.Aggregates;
using Rides.Domain.Commands;
using Rides.Domain.ReadModels;

namespace Rides.Application.Handlers;

public class RequestRideHandler : ICommandHandler<RequestRideCommand>
{
    private readonly IRideEventStore eventStore;
    private readonly IRideReadStore rideReadStore;

    public RequestRideHandler(
        IRideEventStore eventStore,
        IRideReadStore rideReadStore)
    {
        this.eventStore = eventStore;
        this.rideReadStore = rideReadStore;
    }

    public async Task Handle(RequestRideCommand command)
    {
        if (await eventStore.Exists(command.RideId, command.TenantId))
        {
            return;
        }

        var riderHasActiveRide = await rideReadStore.HasActiveRideForRider(command.RiderId, command.TenantId);

        if (riderHasActiveRide)
        {
            throw new InvalidOperationException($"Rider {command.RiderId} already has an active ride.");
        }

        var ride = RideAggregate.Start(command);

        await eventStore.Append(ride);

        await rideReadStore.Upsert(RideReadModel.Create(
            ride.Id,
            ride.TenantId,
            ride.RiderId,
            ride.DriverId,
            command.DriverName,
            ride.Fare.Amount,
            ride.Fare.Currency));
    }
}
