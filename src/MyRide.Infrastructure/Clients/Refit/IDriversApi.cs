using MyRide.Infrastructure.Models;
using Refit;

namespace MyRide.Infrastructure.Clients.Refit;

public interface IDriversApi
{
    [Get("/api/v1/drivers/available")]
    Task<AvailableDriverResponse> GetAvailableDriver([Header("X-Tenant-Id")] string tenantId);

    [Post("/api/v1/drivers/{driverId}/assign")]
    Task AssignDriver(Guid driverId, [Body] DriverActionRequest request);

    [Post("/api/v1/drivers/{driverId}/free")]
    Task FreeDriver(Guid driverId, [Body] DriverActionRequest request);
}
