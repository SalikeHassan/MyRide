using SharedKernel;

namespace Payouts.Domain.Events;

public class DriverPaid : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string TenantId { get; }
    public Guid PayoutId { get; }
    public Guid RecipientId { get; }
    public decimal Amount { get; }
    public string Currency { get; }

    public DriverPaid(string tenantId, Guid payoutId, Guid recipientId, decimal amount, string currency)
    {
        TenantId = tenantId;
        PayoutId = payoutId;
        RecipientId = recipientId;
        Amount = amount;
        Currency = currency;
    }
}