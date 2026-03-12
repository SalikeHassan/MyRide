namespace Payments.Domain.Commands;

public class ChargeRiderCommand
{
    public Guid PaymentId { get; }
    public string TenantId { get; }
    public Guid PayerId { get; }
    public Guid PayeeId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public bool SimulateFailure { get; }

    public ChargeRiderCommand(Guid paymentId, string tenantId, Guid payerId, Guid payeeId, decimal amount, string currency,
        bool simulateFailure = false)
    {
        PaymentId = paymentId;
        TenantId = tenantId;
        PayerId = payerId;
        PayeeId = payeeId;
        Amount = amount;
        Currency = currency;
        SimulateFailure = simulateFailure;
    }
}