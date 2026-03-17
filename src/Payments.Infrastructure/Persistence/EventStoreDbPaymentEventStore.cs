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

    public Task<PaymentAggregate> Load(Guid paymentId, string tenantId) => LoadEvents(paymentId, tenantId);
}
