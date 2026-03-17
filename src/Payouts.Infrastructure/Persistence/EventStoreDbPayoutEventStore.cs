using Common.Infrastructure;
using EventStore.Client;
using Payouts.Application.Ports;
using Payouts.Domain.Aggregates;
using Payouts.Domain.Events;

namespace Payouts.Infrastructure.Persistence;

public class EventStoreDbPayoutEventStore : EventStoreDbEventStore<PayoutAggregate>, IPayoutEventStore
{
    protected override string StreamPrefix => "payout";

    protected override Dictionary<string, Type> EventTypeMap => new()
    {
        { nameof(DriverPaid),      typeof(DriverPaid) },
        { nameof(DriverPayFailed), typeof(DriverPayFailed) }
    };

    public EventStoreDbPayoutEventStore(EventStoreClient client)
        : base(client, PayoutAggregate.Load) { }

    public Task Append(PayoutAggregate payout) => AppendEvents(payout);

    public Task<PayoutAggregate> Load(Guid payoutId, string tenantId) => LoadEvents(payoutId, tenantId);
}
