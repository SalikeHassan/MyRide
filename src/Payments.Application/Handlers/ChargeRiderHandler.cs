using Payments.Application.Ports;
using Payments.Domain.Aggregate;
using Payments.Domain.Commands;

namespace Payments.Application.Handlers;

public class ChargeRiderHandler
{
    private readonly IPaymentEventStore eventStore;

    public ChargeRiderHandler(IPaymentEventStore eventStore)
    {
        this.eventStore = eventStore;
    }

    public async Task<Guid> Handle(ChargeRiderCommand command)
    {
        if (await eventStore.ExistsByRideId(command.RideId, command.TenantId))
        {
            var existing = await eventStore.LoadByRideId(command.RideId, command.TenantId);
            return existing.Id;
        }

        var payment = PaymentAggregate.Charge(command);

        await eventStore.AppendWithRideId(payment, command.RideId);

        return payment.Id;
    }
}
