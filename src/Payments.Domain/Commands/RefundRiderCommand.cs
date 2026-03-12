namespace Payments.Domain.Commands;

public class RefundRiderCommand
{
    public Guid PaymentId { get; }
    public string TenantId { get; }

    public RefundRiderCommand(Guid paymentId, string tenantId)
    {
        PaymentId = paymentId;
        TenantId = tenantId;
    }
}