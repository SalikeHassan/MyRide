using Common.Domain;

namespace Payments.Domain.Commands;

public class ChargeRiderCommand : ICommand
{
    public Guid RideId { get; }
    public Guid PaymentId { get; }
    public string TenantId { get; }
    public Guid PayerId { get; }
    public Guid PayeeId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public bool SimulateFailure { get; }

    public ChargeRiderCommand(Guid rideId, string tenantId, Guid payerId, Guid payeeId, decimal amount, string currency,
        bool simulateFailure = false)
    {
        RideId = rideId;
        PaymentId = Guid.NewGuid();
        TenantId = tenantId;
        PayerId = payerId;
        PayeeId = payeeId;
        Amount = amount;
        Currency = currency;
        SimulateFailure = simulateFailure;
    }
}