using Refit;

namespace MyRide.API.Clients;

public interface IPaymentsApi
{
    [Post("/api/v1/payments/charge")]
    Task<ChargeRiderResponse> ChargeRiderAsync([Body] ChargeRiderRequest request, [Header("X-Tenant-Id")] string tenantId);

    [Post("/api/v1/payments/{paymentId}/refund")]
    Task RefundRiderAsync(Guid paymentId, [Header("X-Tenant-Id")] string tenantId);
}

public record ChargeRiderRequest(
    Guid PayerId,
    Guid PayeeId,
    decimal Amount,
    string Currency,
    bool SimulateFailure = false);

public record ChargeRiderResponse(Guid PaymentId, string Message);
