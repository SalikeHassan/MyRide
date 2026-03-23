namespace MyRide.Application.Ports;

public interface IDownstreamPaymentsClient
{
    Task<Guid> ChargeRider(Guid rideId, Guid riderId, Guid driverId, decimal amount, string currency, string tenantId);
}
