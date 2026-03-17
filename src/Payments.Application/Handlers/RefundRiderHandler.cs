using Common.Application;
using Payments.Application.Ports;
using Payments.Domain.Commands;

namespace Payments.Application.Handlers;

public class RefundRiderHandler : ICommandHandler<RefundRiderCommand>
{
    private readonly IPaymentEventStore eventStore;
    private readonly IPaymentEventPublisher eventPublisher;

    public RefundRiderHandler(IPaymentEventStore eventStore,
        IPaymentEventPublisher eventPublisher)
    {
        this.eventStore = eventStore;
        this.eventPublisher = eventPublisher;
    }

    public async Task Handle(RefundRiderCommand command)
    {
        var payment = await eventStore.Load(command.PaymentId,
            command.TenantId);

        payment.Refund();

        await eventStore.Append(payment);

        foreach (var domainEvent in payment.DomainEvents)
        {
            await eventPublisher.Publish(domainEvent);
        }

        payment.ClearDomainEvents();
    }
}