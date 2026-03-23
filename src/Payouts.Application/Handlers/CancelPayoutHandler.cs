using Common.Application;
using Payouts.Application.Ports;
using Payouts.Domain.Commands;

namespace Payouts.Application.Handlers;

public class CancelPayoutHandler : ICommandHandler<CancelPayoutCommand>
{
    private readonly IPayoutEventStore eventStore;

    public CancelPayoutHandler(IPayoutEventStore eventStore)
    {
        this.eventStore = eventStore;
    }

    public async Task Handle(CancelPayoutCommand command)
    {
        var payout = await eventStore.Load(command.PayoutId, command.TenantId);

        payout.Cancel(command.Reason);

        await eventStore.Append(payout);
    }
}