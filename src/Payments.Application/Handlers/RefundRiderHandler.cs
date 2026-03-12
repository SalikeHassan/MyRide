using Payments.Application.Ports;
using Payments.Domain.Commands;

namespace Payments.Application.Handlers;

public class RefundRiderHandler
{
    private readonly IPaymentEventStore eventStore;
    private readonly IPaymentEventPublisher eventPublisher;

    public RefundRiderHandler(IPaymentEventStore eventStore,
        IPaymentEventPublisher eventPublisher)
    {
        this.eventStore = eventStore;
        this.eventPublisher = eventPublisher;
    }

    public async Task HandleAsync(RefundRiderCommand command)
    {
        var payment = await eventStore.LoadAsync(command.PaymentId,
            command.TenantId);

        payment.Refund();

        await eventStore.AppendAsync(payment);

        foreach (var domainEvent in payment.DomainEvents)
        {
            await eventPublisher.PublishAsync(domainEvent);
        }

        payment.ClearDomainEvents();
    }
}