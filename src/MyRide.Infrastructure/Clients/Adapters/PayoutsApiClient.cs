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

    public Task PayDriver(Guid driverId, decimal amount, string currency, string tenantId)
    {
        return payoutsApi.PayDriver(new PayDriverRequest(driverId, amount, currency), tenantId);
    }
}
