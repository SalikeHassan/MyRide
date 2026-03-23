using Payouts.Domain.Aggregates;

namespace Payouts.Application.Ports;

public interface IPayoutEventStore
{
    Task Append(PayoutAggregate payout);
    Task AppendWithRideId(PayoutAggregate payout, Guid rideId);
    Task<PayoutAggregate> Load(Guid payoutId, string tenantId);
    Task<PayoutAggregate> LoadByRideId(Guid rideId, string tenantId);
    Task<bool> ExistsByRideId(Guid rideId, string tenantId);
}
