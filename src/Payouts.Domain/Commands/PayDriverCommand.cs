using Common.Domain;

namespace Payouts.Domain.Commands;

public class PayDriverCommand : ICommand
{
    public Guid PayoutId { get; }
    public string TenantId { get; }
    public Guid RecipientId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public bool SimulateFailure { get; }

    public PayDriverCommand(Guid payoutId, string tenantId, Guid recipientId, decimal amount, string currency, bool 
        simulateFailure = false)
    {
        PayoutId = payoutId;
        TenantId = tenantId;
        RecipientId = recipientId;
        Amount = amount;
        Currency = currency;
        SimulateFailure = simulateFailure;
    }
}