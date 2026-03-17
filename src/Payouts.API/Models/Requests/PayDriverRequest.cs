namespace Payouts.API.Models.Requests;

public record PayDriverRequest(
    Guid RecipientId,
    decimal Amount,
    string Currency,
    bool SimulateFailure = false);
