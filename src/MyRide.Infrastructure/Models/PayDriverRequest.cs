namespace MyRide.Infrastructure.Models;

public record PayDriverRequest(
    Guid RecipientId,
    decimal Amount,
    string Currency,
    bool SimulateFailure = false);
