using Payouts.Domain.Aggregates;

namespace Payouts.Application.Ports;

public interface IPayoutEventStore                        
{
    Task Append(PayoutAggregate payout);
    Task<PayoutAggregate> Load(Guid payoutId, string tenantId);
}