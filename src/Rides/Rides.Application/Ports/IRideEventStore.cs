using Rides.Domain.Aggregates;

namespace Rides.Application.Ports;

public interface IRideEventStore
{
    Task AppendAsync(RideAggregate ride);
    Task<RideAggregate> LoadAsync(Guid rideId, string tenantId);
}
