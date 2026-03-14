using Refit;

namespace MyRide.API.Clients;

public interface IRidesApi
{
    [Get("/api/v1/rides/active")]
    Task<List<ActiveRideResponse>> GetActiveRidesAsync([Header("X-Tenant-Id")] string tenantId);

    [Post("/api/v1/rides/start")]
    Task<StartRideResponse> StartRideAsync([Body] StartRideRequest request, [Header("X-Tenant-Id")] string tenantId);

    [Post("/api/v1/rides/{rideId}/accept")]
    Task AcceptRideAsync(Guid rideId, [Header("X-Tenant-Id")] string tenantId);

    [Post("/api/v1/rides/{rideId}/complete")]
    Task CompleteRideAsync(Guid rideId, [Header("X-Tenant-Id")] string tenantId);

    [Post("/api/v1/rides/{rideId}/cancel")]
    Task CancelRideAsync(Guid rideId, [Body] CancelRideRequest request, [Header("X-Tenant-Id")] string tenantId);
}

public record StartRideRequest(
    Guid RiderId,
    Guid DriverId,
    string DriverName,
    decimal FareAmount,
    string FareCurrency,
    double PickupLat,
    double PickupLng,
    double DropoffLat,
    double DropoffLng);

public record StartRideResponse(Guid RideId, string Message);

public record CancelRideRequest(string Reason);

public record ActiveRideResponse(
    Guid RideId,
    string TenantId,
    Guid RiderId,
    Guid DriverId,
    string DriverName,
    string Status,
    decimal FareAmount,
    string FareCurrency,
    DateTime LastUpdatedOn);
