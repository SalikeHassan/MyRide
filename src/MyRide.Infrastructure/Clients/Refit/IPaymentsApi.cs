using MyRide.Infrastructure.Models;
using Refit;

namespace MyRide.Infrastructure.Clients.Refit;

public interface IPaymentsApi
{
    [Post("/api/v1/payments/charge")]
    Task<ChargeRiderResponse> ChargeRider([Body] ChargeRiderRequest request, [Header("X-Tenant-Id")] string tenantId);

    [Post("/api/v1/payments/{paymentId}/refund")]
    Task RefundRider(Guid paymentId, [Header("X-Tenant-Id")] string tenantId);
}
