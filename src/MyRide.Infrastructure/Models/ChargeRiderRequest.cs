namespace MyRide.Infrastructure.Models;

public record ChargeRiderRequest(
    Guid RideId,
    Guid PayerId,
    Guid PayeeId,
    decimal Amount,
    string Currency,
    bool SimulateFailure = false);
