namespace MyRide.Application.Ports;

public interface IDownstreamPaymentsClient
{
    Task<Guid> ChargeRider(Guid riderId, Guid driverId, decimal amount, string currency, string tenantId);
}
