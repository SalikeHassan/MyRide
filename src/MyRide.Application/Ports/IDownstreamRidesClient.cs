using MyRide.Application.Models;

namespace MyRide.Application.Ports;

public interface IDownstreamRidesClient
{
    Task<RideResult?> GetRide(Guid rideId, string tenantId);
    Task<Guid> StartRide(StartRideData data, string tenantId);
    Task CompleteRide(Guid rideId, string tenantId);
}
