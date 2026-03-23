using Rides.Domain.Aggregates;

namespace Rides.Application.Ports;

public interface IRideEventStore
{
    Task Append(RideAggregate ride);
    Task<RideAggregate> Load(Guid rideId, string tenantId);
    Task<bool> Exists(Guid rideId, string tenantId);
}
