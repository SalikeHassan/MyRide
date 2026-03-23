namespace MyRide.Infrastructure.Models;

public record PayDriverRequest(
    Guid RideId,
    Guid RecipientId,
    decimal Amount,
    string Currency,
    bool SimulateFailure = false);
