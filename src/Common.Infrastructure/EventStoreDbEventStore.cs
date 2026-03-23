using System.Text.Json;
using Common.Domain;
using EventStore.Client;

namespace Common.Infrastructure;

public abstract class EventStoreDbEventStore<TAggregate> where TAggregate : AggregateRoot
{
    private readonly EventStoreClient client;
    private readonly Func<IEnumerable<IDomainEvent>, TAggregate> factory;

    protected abstract string StreamPrefix { get; }
    protected abstract Dictionary<string, Type> EventTypeMap { get; }

    protected EventStoreDbEventStore(
        EventStoreClient client,
        Func<IEnumerable<IDomainEvent>, TAggregate> factory)
    {
        this.client = client;
        this.factory = factory;
    }

    protected Task AppendEvents(TAggregate aggregate, Guid streamId)
    {
        return AppendEventsToStream(aggregate, StreamName(streamId, aggregate.TenantId));
    }

    protected Task AppendEvents(TAggregate aggregate)
    {
        return AppendEventsToStream(aggregate, StreamName(aggregate.Id, aggregate.TenantId));
    }

    private async Task AppendEventsToStream(TAggregate aggregate, string streamName)
    {
        var eventData = aggregate.DomainEvents.Select(e => new EventData(
            Uuid.NewUuid(),
            e.GetType().Name,
            JsonSerializer.SerializeToUtf8Bytes(e, e.GetType())
        ));

        var priorEventCount = aggregate.Version - aggregate.DomainEvents.Count;

        if (priorEventCount == 0)
        {
            await client.AppendToStreamAsync(streamName, StreamState.NoStream, eventData);
        }
        else
        {
            var expectedRevision = StreamRevision.FromInt64(priorEventCount - 1);
            await client.AppendToStreamAsync(streamName, expectedRevision, eventData);
        }
    }

    protected async Task<TAggregate> LoadEvents(Guid id, string tenantId)
    {
        var streamName = StreamName(id, tenantId);

        var result = client.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.Start);

        if (await result.ReadState == ReadState.StreamNotFound)
        {
            throw new InvalidOperationException($"Stream {streamName} not found.");
        }

        var events = new List<IDomainEvent>();

        await foreach (var resolvedEvent in result)
        {
            events.Add(Deserialize(resolvedEvent));
        }

        return factory(events);
    }

    private IDomainEvent Deserialize(ResolvedEvent resolvedEvent)
    {
        if (!EventTypeMap.TryGetValue(resolvedEvent.Event.EventType, out var type))
        {
            throw new InvalidOperationException($"Unknown event type: {resolvedEvent.Event.EventType}");
        }

        var deserialized = JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, type);

        if (deserialized is not IDomainEvent domainEvent)
        {
            throw new InvalidOperationException($"Failed to deserialize event: {resolvedEvent.Event.EventType}");
        }

        return domainEvent;
    }

    protected async Task<bool> StreamExists(Guid id, string tenantId)
    {
        var streamName = StreamName(id, tenantId);
        var result = client.ReadStreamAsync(Direction.Backwards, streamName, StreamPosition.End, maxCount: 1);
        return await result.ReadState != ReadState.StreamNotFound;
    }

    private string StreamName(Guid id, string tenantId) =>
        $"{tenantId}-{StreamPrefix}-{id}";
}
