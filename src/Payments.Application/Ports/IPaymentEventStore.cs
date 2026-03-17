using Payments.Domain.Aggregate;

namespace Payments.Application.Ports;

public interface IPaymentEventStore
{
    Task Append(PaymentAggregate payment);
    Task<PaymentAggregate> Load(Guid paymentId, string 
        tenantId);
}