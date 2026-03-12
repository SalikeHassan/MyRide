using System.Text.Json;
using MongoDB.Driver;
using Rides.Application.Ports;
using Rides.Domain.Aggregates;
using Rides.Domain.Events;
using SharedKernel;

namespace Rides.Infrastructure.Persistence;

public class MongoRideEventStore : IRideEventStore
{
    private readonly IMongoDatabase database;

    private static readonly Dictionary<string, Type> eventTypeMap = new()
    {
        { nameof(RideStarted), typeof(RideStarted) },
        { nameof(RideAccepted), typeof(RideAccepted) },
        { nameof(RideCompleted), typeof(RideCompleted) },
        { nameof(RideCancelled), typeof(RideCancelled) }
    };

    public MongoRideEventStore(IMongoDatabase database)
    {
        this.database = database;
    }

    public async Task AppendAsync(RideAggregate ride)
    {
        var collection = GetCollection(ride.TenantId);

        var startingVersion = ride.Version - ride.DomainEvents.Count;

        var documents = ride.DomainEvents.Select((e, index) => new EventDocument
        {
            AggregateId = ride.Id,
            TenantId = ride.TenantId,
            EventType = e.GetType().Name,
            EventData = JsonSerializer.Serialize(e, e.GetType()),
            Version = startingVersion + index + 1,
            OccurredOn = e.OccurredOn
        }).ToList();

        await collection.InsertManyAsync(documents);
    }

    public async Task<RideAggregate> LoadAsync(Guid rideId, string tenantId)
    {
        var collection = GetCollection(tenantId);

        var filter = Builders<EventDocument>.Filter.Eq(d => d.AggregateId, rideId);
        var sort = Builders<EventDocument>.Sort.Ascending(d => d.Version);

        var documents = await collection
            .Find(filter)
            .Sort(sort)
            .ToListAsync();

        if (documents.Count == 0)
        {
            throw new InvalidOperationException($"Ride {rideId} not found for tenant {tenantId}.");
        }

        var events = documents.Select(Deserialize).ToList();

        return RideAggregate.Load(events);
    }

    private IDomainEvent Deserialize(EventDocument document)
    {
        if (!eventTypeMap.TryGetValue(document.EventType, out var type))
        {
            throw new InvalidOperationException($"Unknown event type: {document.EventType}");
        }

        var deserialized = JsonSerializer.Deserialize(document.EventData, type);

        if (deserialized is not IDomainEvent domainEvent)
        {
            throw new InvalidOperationException($"Failed to deserialize event: {document.EventType}");
        }

        return domainEvent;
    }

    private IMongoCollection<EventDocument> GetCollection(string tenantId)
    {
        return database.GetCollection<EventDocument>($"{tenantId}_events");
    }
}
