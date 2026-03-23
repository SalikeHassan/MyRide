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

    public Task AppendWithRideId(PayoutAggregate payout, Guid rideId) => AppendEvents(payout, rideId);

    public Task<PayoutAggregate> Load(Guid payoutId, string tenantId) => LoadEvents(payoutId, tenantId);

    public Task<PayoutAggregate> LoadByRideId(Guid rideId, string tenantId) => LoadEvents(rideId, tenantId);

    public Task<bool> ExistsByRideId(Guid rideId, string tenantId) => StreamExists(rideId, tenantId);
}
