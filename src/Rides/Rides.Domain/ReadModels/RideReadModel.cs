using Rides.Domain.Aggregates;

namespace Rides.Domain.ReadModels;

public class RideReadModel
{
    public Guid RideId { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public Guid RiderId { get; private set; }
    public Guid DriverId { get; private set; }
    public string DriverName { get; private set; } = string.Empty;
    public RideStatus Status { get; private set; }
    public decimal FareAmount { get; private set; }
    public string FareCurrency { get; private set; } = string.Empty;
    public DateTime LastUpdatedOn { get; private set; }

    private RideReadModel() { }

    public static RideReadModel Create(
        Guid rideId,
        string tenantId,
        Guid riderId,
        Guid driverId,
        string driverName,
        decimal fareAmount,
        string fareCurrency)
    {
        return new RideReadModel
        {
            RideId = rideId,
            TenantId = tenantId,
            RiderId = riderId,
            DriverId = driverId,
            DriverName = driverName,
            Status = RideStatus.Requested,
            FareAmount = fareAmount,
            FareCurrency = fareCurrency,
            LastUpdatedOn = DateTime.UtcNow
        };
    }

    public void Accept()
    {
        Status = RideStatus.InProgress;
        LastUpdatedOn = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = RideStatus.Completed;
        LastUpdatedOn = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = RideStatus.Cancelled;
        LastUpdatedOn = DateTime.UtcNow;
    }
}
