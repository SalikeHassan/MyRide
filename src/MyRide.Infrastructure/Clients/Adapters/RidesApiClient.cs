using MyRide.Application.Models;
using MyRide.Application.Ports;
using MyRide.Infrastructure.Clients.Refit;
using MyRide.Infrastructure.Models;

namespace MyRide.Infrastructure.Clients.Adapters;

public class RidesApiClient : IDownstreamRidesClient
{
    private readonly IRidesApi ridesApi;

    public RidesApiClient(IRidesApi ridesApi)
    {
        this.ridesApi = ridesApi;
    }

    public async Task<RideResult?> GetRide(Guid rideId, string tenantId)
    {
        try
        {
            var response = await ridesApi.GetRide(rideId, tenantId);
            return new RideResult(
                response.RideId,
                response.RiderId,
                response.DriverId,
                response.FareAmount,
                response.FareCurrency);
        }
        catch
        {
            return null;
        }
    }

    public async Task<Guid> StartRide(StartRideData data, string tenantId)
    {
        var request = new StartRideRequest(
            data.RideId,
            data.RiderId,
            data.DriverId,
            data.DriverName,
            data.FareAmount,
            data.FareCurrency,
            data.PickupLat,
            data.PickupLng,
            data.DropoffLat,
            data.DropoffLng);

        var response = await ridesApi.StartRide(request, tenantId);
        return response.RideId;
    }

    public Task CompleteRide(Guid rideId, string tenantId)
    {
        return ridesApi.CompleteRide(rideId, tenantId);
    }
}
