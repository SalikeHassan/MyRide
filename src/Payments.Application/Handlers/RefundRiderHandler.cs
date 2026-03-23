using Common.Application;
using Payments.Application.Ports;
using Payments.Domain.Commands;

namespace Payments.Application.Handlers;

public class RefundRiderHandler : ICommandHandler<RefundRiderCommand>
{
    private readonly IPaymentEventStore eventStore;

    public RefundRiderHandler(IPaymentEventStore eventStore)
    {
        this.eventStore = eventStore;
    }

    public async Task Handle(RefundRiderCommand command)
    {
        var payment = await eventStore.Load(command.PaymentId, command.TenantId);

        payment.Refund();

        await eventStore.Append(payment);
    }
}