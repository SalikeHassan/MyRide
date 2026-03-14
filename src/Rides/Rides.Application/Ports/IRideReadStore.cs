using Rides.Domain.Aggregates;

namespace Rides.Application.Ports;

public interface IRideReadStore
{
    Task<bool> HasActiveRideForRiderAsync(Guid riderId, string tenantId);
    Task<bool> HasActiveRideForDriverAsync(Guid driverId, string tenantId);
    Task UpsertAsync(RideReadModel readModel);
    Task<List<RideReadModel>> GetActiveRidesAsync(string tenantId);
}

public class RideReadModel
{
    public Guid RideId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid RiderId { get; set; }
    public Guid DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public RideStatus Status { get; set; }
    public decimal FareAmount { get; set; }
    public string FareCurrency { get; set; } = string.Empty;
    public DateTime LastUpdatedOn { get; set; }
}
