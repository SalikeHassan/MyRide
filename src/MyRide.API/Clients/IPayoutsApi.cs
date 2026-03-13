using Refit;

namespace MyRide.API.Clients;

public interface IPayoutsApi
{
    [Post("/api/v1/payouts/pay")]
    Task PayDriverAsync([Body] PayDriverRequest request, [Header("X-Tenant-Id")] string tenantId);

    [Post("/api/v1/payouts/{payoutId}/cancel")]
    Task CancelPayoutAsync(Guid payoutId, [Body] CancelPayoutRequest request, [Header("X-Tenant-Id")] string tenantId);
}

public record PayDriverRequest(
    Guid RecipientId,
    decimal Amount,
    string Currency,
    bool SimulateFailure = false);

public record CancelPayoutRequest(string Reason);
