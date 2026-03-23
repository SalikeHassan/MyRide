namespace MyRide.Domain.Sagas;

public class RequestRideSagaState
{
    public Guid SagaId { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public Guid RideId { get; private set; }
    public Guid DriverId { get; private set; }
    public Guid RiderId { get; private set; }
    public string DriverName { get; private set; } = string.Empty;
    public decimal FareAmount { get; private set; }
    public string FareCurrency { get; private set; } = string.Empty;
    public double PickupLat { get; private set; }
    public double PickupLng { get; private set; }
    public double DropoffLat { get; private set; }
    public double DropoffLng { get; private set; }
    public RequestRideSagaStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private RequestRideSagaState() { }

    public static RequestRideSagaState Create(
        Guid rideId,
        Guid driverId,
        Guid riderId,
        string driverName,
        string tenantId,
        decimal fareAmount,
        string fareCurrency,
        double pickupLat,
        double pickupLng,
        double dropoffLat,
        double dropoffLng)
    {
        return new RequestRideSagaState
        {
            SagaId = Guid.NewGuid(),
            RideId = rideId,
            DriverId = driverId,
            RiderId = riderId,
            DriverName = driverName,
            TenantId = tenantId,
            FareAmount = fareAmount,
            FareCurrency = fareCurrency,
            PickupLat = pickupLat,
            PickupLng = pickupLng,
            DropoffLat = dropoffLat,
            DropoffLng = dropoffLng,
            Status = RequestRideSagaStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void MarkDriverAssigned()
    {
        Status = RequestRideSagaStatus.DriverAssigned;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        Status = RequestRideSagaStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = RequestRideSagaStatus.Failed;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompensating(string reason)
    {
        Status = RequestRideSagaStatus.Compensating;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompensated()
    {
        Status = RequestRideSagaStatus.Compensated;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompensationFailed(string reason)
    {
        Status = RequestRideSagaStatus.CompensationFailed;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }
}
