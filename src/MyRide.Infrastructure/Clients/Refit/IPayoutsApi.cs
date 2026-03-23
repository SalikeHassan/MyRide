using MyRide.Infrastructure.Models;
using Refit;

namespace MyRide.Infrastructure.Clients.Refit;

public interface IPayoutsApi
{
    [Post("/api/v1/payouts/pay")]
    Task<PayDriverResponse> PayDriver([Body] PayDriverRequest request, [Header("X-Tenant-Id")] string tenantId);

    [Post("/api/v1/payouts/{payoutId}/cancel")]
    Task CancelPayout(Guid payoutId, [Body] CancelPayoutRequest request, [Header("X-Tenant-Id")] string tenantId);
}
