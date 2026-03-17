using Common.Application;
using Payouts.Application.Ports;
using Payouts.Domain.Aggregates;
using Payouts.Domain.Commands;

namespace Payouts.Application.Handlers;

public class PayDriverHandler : ICommandHandler<PayDriverCommand>
{
    private readonly IPayoutEventStore eventStore;
    private readonly IPayoutEventPublisher eventPublisher;

    public PayDriverHandler(IPayoutEventStore eventStore, IPayoutEventPublisher eventPublisher)
    {
        this.eventStore = eventStore;
        this.eventPublisher = eventPublisher;
    }

    public async Task Handle(PayDriverCommand command)
    {
        var payout = PayoutAggregate.Pay(command);

        await eventStore.Append(payout);

        foreach (var domainEvent in payout.DomainEvents)
        {
            await eventPublisher.Publish(domainEvent);
        }

        payout.ClearDomainEvents();
    }
}