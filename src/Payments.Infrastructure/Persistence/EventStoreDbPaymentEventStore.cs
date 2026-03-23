using Common.Infrastructure;
using EventStore.Client;
using Payments.Application.Ports;
using Payments.Domain.Aggregate;
using Payments.Domain.Events;

namespace Payments.Infrastructure.Persistence;

public class EventStoreDbPaymentEventStore : EventStoreDbEventStore<PaymentAggregate>, IPaymentEventStore
{
    protected override string StreamPrefix => "payment";

    protected override Dictionary<string, Type> EventTypeMap => new()
    {
        { nameof(RiderCharged),      typeof(RiderCharged) },
        { nameof(RiderChargeFailed), typeof(RiderChargeFailed) },
        { nameof(RiderRefunded),     typeof(RiderRefunded) }
    };

    public EventStoreDbPaymentEventStore(EventStoreClient client)
        : base(client, PaymentAggregate.Load) { }

    public Task Append(PaymentAggregate payment) => AppendEvents(payment);

    public Task AppendWithRideId(PaymentAggregate payment, Guid rideId) => AppendEvents(payment, rideId);

    public Task<PaymentAggregate> Load(Guid paymentId, string tenantId) => LoadEvents(paymentId, tenantId);

    public Task<PaymentAggregate> LoadByRideId(Guid rideId, string tenantId) => LoadEvents(rideId, tenantId);

    public Task<bool> ExistsByRideId(Guid rideId, string tenantId) => StreamExists(rideId, tenantId);
}
