using MyRide.Application.Ports;
using MyRide.Infrastructure.Clients.Refit;
using MyRide.Infrastructure.Models;

namespace MyRide.Infrastructure.Clients.Adapters;

public class PayoutsApiClient : IDownstreamPayoutsClient
{
    private readonly IPayoutsApi payoutsApi;

    public PayoutsApiClient(IPayoutsApi payoutsApi)
    {
        this.payoutsApi = payoutsApi;
    }

    public async Task<Guid> PayDriver(Guid rideId, Guid driverId, decimal amount, string currency, string tenantId)
    {
        var response = await payoutsApi.PayDriver(
            new PayDriverRequest(rideId, driverId, amount, currency),
            tenantId);

        return response.PayoutId;
    }
}
