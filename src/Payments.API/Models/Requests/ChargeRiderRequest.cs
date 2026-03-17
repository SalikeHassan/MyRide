namespace Payments.API.Models.Requests;

public record ChargeRiderRequest(
    Guid PayerId,
    Guid PayeeId,
    decimal Amount,
    string Currency,
    bool SimulateFailure = false);
