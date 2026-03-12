using Rides.Domain.Aggregates;

namespace Rides.Application.Ports;

public interface IRideReadStore
{
    Task<bool> HasActiveRideForRiderAsync(Guid riderId, string tenantId);
    Task<bool> HasActiveRideForDriverAsync(Guid driverId, string tenantId);
    Task UpsertAsync(RideReadModel readModel);
}

public class RideReadModel
{
    public Guid RideId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid RiderId { get; set; }
    public Guid DriverId { get; set; }
    public RideStatus Status { get; set; }
    public DateTime LastUpdatedOn { get; set; }
}
