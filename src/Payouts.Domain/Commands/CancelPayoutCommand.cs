namespace Payouts.Domain.Commands;

public class CancelPayoutCommand
{
    public Guid PayoutId { get; }
    public string TenantId { get; }
    public string Reason { get; }

    public CancelPayoutCommand(Guid payoutId, string tenantId, string reason)
    {
        PayoutId = payoutId;
        TenantId = tenantId;
        Reason = reason;
    }
}