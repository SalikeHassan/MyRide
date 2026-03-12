using SharedKernel;

namespace Payments.Domain.Events;

public class RiderRefunded : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string TenantId { get; }
    public Guid PaymentId { get; }
    public Guid PayerId { get; }
    public decimal Amount { get; }
    public string Currency { get; }

    public RiderRefunded(string tenantId, Guid paymentId, Guid payerId, decimal amount, string currency)
    {
        TenantId = tenantId;
        PaymentId = paymentId;
        PayerId = payerId;
        Amount = amount;
        Currency = currency;
    }
}