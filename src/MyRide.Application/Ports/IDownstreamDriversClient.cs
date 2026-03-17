using MyRide.Application.Models;

namespace MyRide.Application.Ports;

public interface IDownstreamDriversClient
{
    Task<AvailableDriverResult?> GetAvailableDriver(string tenantId);
    Task AssignDriver(Guid driverId, Guid rideId, string tenantId);
    Task FreeDriver(Guid driverId, Guid rideId, string tenantId);
}
