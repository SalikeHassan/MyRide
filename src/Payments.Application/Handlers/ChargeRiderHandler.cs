using Common.Application;
using Payments.Application.Ports;
using Payments.Domain.Aggregate;
using Payments.Domain.Commands;

namespace Payments.Application.Handlers;

public class ChargeRiderHandler : ICommandHandler<ChargeRiderCommand>
{
    private readonly IPaymentEventStore eventStore;
    private readonly IPaymentEventPublisher eventPublisher;

    public ChargeRiderHandler(IPaymentEventStore eventStore,
        IPaymentEventPublisher eventPublisher)
    {
        this.eventStore = eventStore;
        this.eventPublisher = eventPublisher;
    }

    public async Task Handle(ChargeRiderCommand command)
    {
        var payment = PaymentAggregate.Charge(command);

        await eventStore.Append(payment);

        foreach (var domainEvent in payment.DomainEvents)
        {
            await eventPublisher.Publish(domainEvent);
        }

        payment.ClearDomainEvents();
    }
}