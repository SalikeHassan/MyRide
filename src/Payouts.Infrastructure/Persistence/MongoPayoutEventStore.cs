using System.Text.Json;
using MongoDB.Driver;
using Payouts.Application.Ports;
using Payouts.Domain.Aggregates;
using Payouts.Domain.Events;
using SharedKernel;

namespace Payouts.Infrastructure.Persistence;

public class MongoPayoutEventStore : IPayoutEventStore
  {
      private readonly IMongoDatabase database;

      private static readonly Dictionary<string, Type> eventTypeMap = new()
      {
          { nameof(DriverPaid), typeof(DriverPaid) },
          { nameof(DriverPayFailed), typeof(DriverPayFailed) }
      };

      public MongoPayoutEventStore(IMongoDatabase database)
      {
          this.database = database;
      }

      public async Task AppendAsync(PayoutAggregate payout)
      {
          var collection = GetCollection(payout.TenantId);

          var startingVersion = payout.Version - payout.DomainEvents.Count;

          var documents = payout.DomainEvents.Select((e, index) => new EventDocument
          {
              AggregateId = payout.Id,
              TenantId = payout.TenantId,
              EventType = e.GetType().Name,
              EventData = JsonSerializer.Serialize(e, e.GetType()),
              Version = startingVersion + index + 1,
              OccurredOn = e.OccurredOn
          }).ToList();

          await collection.InsertManyAsync(documents);
      }

      public async Task<PayoutAggregate> LoadAsync(Guid payoutId, string tenantId)
      {
          var collection = GetCollection(tenantId);

          var filter = Builders<EventDocument>.Filter.Eq(d => d.AggregateId, payoutId);
          var sort = Builders<EventDocument>.Sort.Ascending(d => d.Version);

          var documents = await collection.Find(filter).Sort(sort).ToListAsync();

          if (documents.Count == 0)
          {
              throw new InvalidOperationException($"Payout {payoutId} not found for tenant {tenantId}.");
          }

          var events = documents.Select(Deserialize).ToList();

          return PayoutAggregate.Load(events);
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
          return database.GetCollection<EventDocument>($"{tenantId}_payouts_events");
      }
  }
