using SharedKernel;

namespace Payouts.Domain.Events;

public class DriverPayFailed : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string TenantId { get; }
    public Guid PayoutId { get; }
    public Guid RecipientId { get; }
    public string Reason { get; }

    public DriverPayFailed(string tenantId, Guid payoutId, Guid recipientId, string reason)
    {
        TenantId = tenantId;
        PayoutId = payoutId;
        RecipientId = recipientId;
        Reason = reason;
    }
}