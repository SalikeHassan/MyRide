using Refit;

namespace MyRide.API.Clients;

public interface IDriversApi
{
    [Get("/api/v1/drivers/available")]
    Task<AvailableDriverResponse> GetAvailableDriverAsync([Header("X-Tenant-Id")] string tenantId);
}

public record AvailableDriverResponse(Guid Id, string Name, string Phone);
