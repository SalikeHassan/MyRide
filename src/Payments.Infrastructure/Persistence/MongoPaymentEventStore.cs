using System.Text.Json;
using MongoDB.Driver;
using Payments.Application.Ports;
using Payments.Domain.Aggregate;
using Payments.Domain.Events;
using SharedKernel;

namespace Payments.Infrastructure.Persistence;

public class MongoPaymentEventStore : IPaymentEventStore
  {
      private readonly IMongoDatabase database;

      private static readonly Dictionary<string, Type> eventTypeMap =
       new()
          {
              { nameof(RiderCharged), typeof(RiderCharged) },
              { nameof(RiderChargeFailed), typeof(RiderChargeFailed) },
              { nameof(RiderRefunded), typeof(RiderRefunded) }
          };

      public MongoPaymentEventStore(IMongoDatabase database)
      {
          this.database = database;
      }

      public async Task AppendAsync(PaymentAggregate payment)
      {
          var collection = GetCollection(payment.TenantId);

          var startingVersion = payment.Version - payment.DomainEvents.Count;

          var documents = payment.DomainEvents.Select((e, index) =>
          new EventDocument
                  {
                      AggregateId = payment.Id,
                      TenantId = payment.TenantId,
                      EventType = e.GetType().Name,
                      EventData = JsonSerializer.Serialize(e, e.GetType()),
                      Version = startingVersion + index + 1,
                      OccurredOn = e.OccurredOn
                  }).ToList();

          await collection.InsertManyAsync(documents);
      }

      public async Task<PaymentAggregate> LoadAsync(Guid paymentId, string tenantId)
      {
          var collection = GetCollection(tenantId);

          var filter = Builders<EventDocument>.Filter.Eq(d => d.AggregateId, paymentId);
          var sort = Builders<EventDocument>.Sort.Ascending(d => d.Version);

          var documents = await collection.Find(filter).Sort(sort).ToListAsync();

          if (documents.Count == 0)
          {
              throw new InvalidOperationException($"Payment{paymentId} not found for tenant {tenantId}.");
          }

          var events = documents.Select(Deserialize).ToList();

          return PaymentAggregate.Load(events);
      }

      private IDomainEvent Deserialize(EventDocument document)
      {
          if (!eventTypeMap.TryGetValue(document.EventType, out var type))
          {
              throw new InvalidOperationException($"Unknown eventtype: {document.EventType}");
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
          return database.GetCollection<EventDocument>($"{tenantId}_payments_events");
      }
  }