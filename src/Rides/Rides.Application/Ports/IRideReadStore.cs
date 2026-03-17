using Rides.Domain.ReadModels;

namespace Rides.Application.Ports;

public interface IRideReadStore
{
    Task<bool> HasActiveRideForRider(Guid riderId, string tenantId);
    Task<RideReadModel?> GetById(Guid rideId, string tenantId);
    Task Upsert(RideReadModel readModel);
    Task<List<RideReadModel>> GetActiveRides(string tenantId);
}
