using MyRide.Infrastructure.Models;
using Refit;

namespace MyRide.Infrastructure.Clients.Refit;

public interface IRidesApi
{
    [Get("/api/v1/rides/active")]
    Task<List<ActiveRideResponse>> GetActiveRides([Header("X-Tenant-Id")] string tenantId);

    [Get("/api/v1/rides/{rideId}")]
    Task<ActiveRideResponse> GetRide(Guid rideId, [Header("X-Tenant-Id")] string tenantId);

    [Post("/api/v1/rides/start")]
    Task<StartRideResponse> StartRide([Body] StartRideRequest request, [Header("X-Tenant-Id")] string tenantId);

    [Post("/api/v1/rides/{rideId}/accept")]
    Task AcceptRide(Guid rideId, [Header("X-Tenant-Id")] string tenantId);

    [Post("/api/v1/rides/{rideId}/complete")]
    Task CompleteRide(Guid rideId, [Header("X-Tenant-Id")] string tenantId);

    [Post("/api/v1/rides/{rideId}/cancel")]
    Task CancelRide(Guid rideId, [Body] CancelRideRequest request, [Header("X-Tenant-Id")] string tenantId);
}
