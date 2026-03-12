using SharedKernel;

namespace Payments.Domain.Events;

public class RiderChargeFailed : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string TenantId { get; }
    public Guid PaymentId { get; }
    public Guid PayerId { get; }
    public string Reason { get; }

    public RiderChargeFailed(string tenantId, Guid paymentId, Guid payerId, string reason)
    {
        TenantId = tenantId;
        PaymentId = paymentId;
        PayerId = payerId;
        Reason = reason;
    }
}