using MyRide.Application.Ports;
using MyRide.Infrastructure.Clients.Refit;
using MyRide.Infrastructure.Models;

namespace MyRide.Infrastructure.Clients.Adapters;

public class PaymentsApiClient : IDownstreamPaymentsClient
{
    private readonly IPaymentsApi paymentsApi;

    public PaymentsApiClient(IPaymentsApi paymentsApi)
    {
        this.paymentsApi = paymentsApi;
    }

    public async Task<Guid> ChargeRider(Guid rideId, Guid riderId, Guid driverId, decimal amount, string currency, string tenantId)
    {
        var response = await paymentsApi.ChargeRider(
            new ChargeRiderRequest(rideId, riderId, driverId, amount, currency),
            tenantId);

        return response.PaymentId;
    }
}
