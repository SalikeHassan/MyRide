using Payouts.Application.Ports;
using Payouts.Domain.Aggregates;
using Payouts.Domain.Commands;

namespace Payouts.Application.Handlers;

public class PayDriverHandler
{
    private readonly IPayoutEventStore eventStore;

    public PayDriverHandler(IPayoutEventStore eventStore)
    {
        this.eventStore = eventStore;
    }

    public async Task<Guid> Handle(PayDriverCommand command)
    {
        if (await eventStore.ExistsByRideId(command.RideId, command.TenantId))
        {
            var existing = await eventStore.LoadByRideId(command.RideId, command.TenantId);
            return existing.Id;
        }

        var payout = PayoutAggregate.Pay(command);

        await eventStore.AppendWithRideId(payout, command.RideId);

        return payout.Id;
    }
}
