namespace MyRide.Application.Ports;

public interface IDownstreamPayoutsClient
{
    Task<Guid> PayDriver(Guid rideId, Guid driverId, decimal amount, string currency, string tenantId);
}
