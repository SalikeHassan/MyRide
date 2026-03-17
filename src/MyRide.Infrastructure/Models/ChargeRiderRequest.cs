namespace MyRide.Infrastructure.Models;

public record ChargeRiderRequest(
    Guid PayerId,
    Guid PayeeId,
    decimal Amount,
    string Currency,
    bool SimulateFailure = false);
