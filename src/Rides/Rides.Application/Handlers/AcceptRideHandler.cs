using Rides.Application.Ports;
using Rides.Domain.Commands;

namespace Rides.Application.Handlers;

public class AcceptRideHandler
{
    private readonly IRideEventStore eventStore;
    private readonly IRideEventPublisher eventPublisher;
    private readonly IRideReadStore rideReadStore;

    public AcceptRideHandler(
        IRideEventStore eventStore,
        IRideEventPublisher eventPublisher,
        IRideReadStore rideReadStore)
    {
        this.eventStore = eventStore;
        this.eventPublisher = eventPublisher;
        this.rideReadStore = rideReadStore;
    }

    public async Task HandleAsync(AcceptRideCommand command)
    {
        var ride = await eventStore.LoadAsync(command.RideId, command.TenantId);

        ride.Accept();

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
            FareAmount = ride.Fare.Amount,
            FareCurrency = ride.Fare.Currency,
            LastUpdatedOn = DateTime.UtcNow
        });

        ride.ClearDomainEvents();
    }
}
