using Common.Domain;
using Common.Infrastructure;
using Drivers.Application.Ports;
using Drivers.Domain.Aggregates;
using Drivers.Domain.Events;
using EventStore.Client;

namespace Drivers.Infrastructure.Persistence;

public class EventStoreDbDriverEventStore : EventStoreDbEventStore<DriverAggregate>, IDriverEventStore
{
    protected override string StreamPrefix => "driver";

    protected override Dictionary<string, Type> EventTypeMap => new()
    {
        { nameof(DriverOnboarded), typeof(DriverOnboarded) },
        { nameof(DriverAssigned), typeof(DriverAssigned) },
        { nameof(DriverFreed), typeof(DriverFreed) }
    };

    public EventStoreDbDriverEventStore(EventStoreClient client)
        : base(client, DriverAggregate.Load) { }

    public Task Append(DriverAggregate driver) => AppendEvents(driver);

    public Task<DriverAggregate> Load(Guid driverId, string tenantId) => LoadEvents(driverId, tenantId);

    public Task<bool> Exists(Guid driverId, string tenantId) => StreamExists(driverId, tenantId);
}
