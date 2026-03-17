using MyRide.Application.Models;
using MyRide.Application.Ports;
using MyRide.Infrastructure.Clients.Refit;
using MyRide.Infrastructure.Models;
using Refit;

namespace MyRide.Infrastructure.Clients.Adapters;

public class DriversApiClient : IDownstreamDriversClient
{
    private readonly IDriversApi driversApi;

    public DriversApiClient(IDriversApi driversApi)
    {
        this.driversApi = driversApi;
    }

    public async Task<AvailableDriverResult?> GetAvailableDriver(string tenantId)
    {
        try
        {
            var response = await driversApi.GetAvailableDriver(tenantId);
            return new AvailableDriverResult(response.Id, response.Name, response.Phone);
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task AssignDriver(Guid driverId, Guid rideId, string tenantId)
    {
        return driversApi.AssignDriver(driverId, new DriverActionRequest(rideId, tenantId));
    }

    public Task FreeDriver(Guid driverId, Guid rideId, string tenantId)
    {
        return driversApi.FreeDriver(driverId, new DriverActionRequest(rideId, tenantId));
    }
}
