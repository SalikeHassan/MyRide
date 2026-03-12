using Payouts.Domain.Aggregates;

namespace Payouts.Application.Ports;

public interface IPayoutEventStore                        
{
    Task AppendAsync(PayoutAggregate payout);
    Task<PayoutAggregate> LoadAsync(Guid payoutId, string tenantId);
}