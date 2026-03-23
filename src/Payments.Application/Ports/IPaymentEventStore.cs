using Payments.Domain.Aggregate;

namespace Payments.Application.Ports;

public interface IPaymentEventStore
{
    Task Append(PaymentAggregate payment);
    Task AppendWithRideId(PaymentAggregate payment, Guid rideId);
    Task<PaymentAggregate> Load(Guid paymentId, string tenantId);
    Task<PaymentAggregate> LoadByRideId(Guid rideId, string tenantId);
    Task<bool> ExistsByRideId(Guid rideId, string tenantId);
}
