using Payouts.Application.Ports;
using Payouts.Domain.Commands;

namespace Payouts.Application.Handlers;

public class CancelPayoutHandler
{
    private readonly IPayoutEventStore eventStore;
    private readonly IPayoutEventPublisher eventPublisher;

    public CancelPayoutHandler(IPayoutEventStore eventStore, IPayoutEventPublisher eventPublisher)
    {
        this.eventStore = eventStore;
        this.eventPublisher = eventPublisher;
    }

    public async Task HandleAsync(CancelPayoutCommand command)
    {
        var payout = await eventStore.LoadAsync(command.PayoutId, command.TenantId);

        payout.Cancel(command.Reason);

        await eventStore.AppendAsync(payout);

        foreach (var domainEvent in payout.DomainEvents)
        {
            await eventPublisher.PublishAsync(domainEvent);
        }

        payout.ClearDomainEvents();
    }
}