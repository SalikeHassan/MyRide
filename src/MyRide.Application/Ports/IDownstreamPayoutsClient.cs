namespace MyRide.Application.Ports;

public interface IDownstreamPayoutsClient
{
    Task PayDriver(Guid driverId, decimal amount, string currency, string tenantId);
}
