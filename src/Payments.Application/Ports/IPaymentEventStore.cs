using Payments.Domain.Aggregate;

namespace Payments.Application.Ports;

public interface IPaymentEventStore
{
    Task AppendAsync(PaymentAggregate payment);
    Task<PaymentAggregate> LoadAsync(Guid paymentId, string 
        tenantId);
}