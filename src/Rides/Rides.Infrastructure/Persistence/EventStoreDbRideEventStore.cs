using Common.Infrastructure;
using EventStore.Client;
using Rides.Application.Ports;
using Rides.Domain.Aggregates;
using Rides.Domain.Events;

namespace Rides.Infrastructure.Persistence;

public class EventStoreDbRideEventStore : EventStoreDbEventStore<RideAggregate>, IRideEventStore
{
    protected override string StreamPrefix => "ride";

    protected override Dictionary<string, Type> EventTypeMap => new()
    {
        { nameof(RideStarted),   typeof(RideStarted) },
        { nameof(RideAccepted),  typeof(RideAccepted) },
        { nameof(RideCompleted), typeof(RideCompleted) },
        { nameof(RideCancelled), typeof(RideCancelled) }
    };

    public EventStoreDbRideEventStore(EventStoreClient client)
        : base(client, RideAggregate.Load) { }

    public Task Append(RideAggregate ride) => AppendEvents(ride);

    public Task<RideAggregate> Load(Guid rideId, string tenantId) => LoadEvents(rideId, tenantId);
}
