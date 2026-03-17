using Common.Application;
using Payouts.Application.Ports;
using Payouts.Domain.Commands;

namespace Payouts.Application.Handlers;

public class CancelPayoutHandler : ICommandHandler<CancelPayoutCommand>
{
    private readonly IPayoutEventStore eventStore;
    private readonly IPayoutEventPublisher eventPublisher;

    public CancelPayoutHandler(IPayoutEventStore eventStore, IPayoutEventPublisher eventPublisher)
    {
        this.eventStore = eventStore;
        this.eventPublisher = eventPublisher;
    }

    public async Task Handle(CancelPayoutCommand command)
    {
        var payout = await eventStore.Load(command.PayoutId, command.TenantId);

        payout.Cancel(command.Reason);

        await eventStore.Append(payout);

        foreach (var domainEvent in payout.DomainEvents)
        {
            await eventPublisher.Publish(domainEvent);
        }

        payout.ClearDomainEvents();
    }
}