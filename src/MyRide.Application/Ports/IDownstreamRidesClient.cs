using MyRide.Application.Models;

namespace MyRide.Application.Ports;

public interface IDownstreamRidesClient
{
    Task<RideResult?> GetRide(Guid rideId, string tenantId);
    Task<Guid> RequestRide(RequestRideData data, string tenantId);
    Task CompleteRide(Guid rideId, string tenantId);
}
