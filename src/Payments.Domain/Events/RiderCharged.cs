using Common.Domain;

namespace Payments.Domain.Events;

public class RiderCharged : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string TenantId { get; }
    public Guid PaymentId { get; }
    public Guid PayerId { get; }
    
    public Guid PayeeId { get; }

    public decimal Amount { get; }
    public string Currency { get; }

    public RiderCharged(string tenantId, Guid paymentId, Guid payerId,Guid payeeId, decimal amount, string currency)
    {
        TenantId = tenantId;
        PaymentId = paymentId;
        PayerId = payerId;
        PayeeId = payeeId;
        Amount = amount;
        Currency = currency;
    }   
}