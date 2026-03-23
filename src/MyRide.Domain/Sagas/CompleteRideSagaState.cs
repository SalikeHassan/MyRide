namespace MyRide.Domain.Sagas;

public class CompleteRideSagaState
{
    public const int MaxRetries = 5;

    public Guid SagaId { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public Guid RideId { get; private set; }
    public Guid DriverId { get; private set; }
    public Guid RiderId { get; private set; }
    public decimal FareAmount { get; private set; }
    public string FareCurrency { get; private set; } = string.Empty;
    public Guid? PaymentId { get; private set; }
    public CompleteRideSagaStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private CompleteRideSagaState() { }

    public static CompleteRideSagaState Create(
        Guid rideId,
        Guid driverId,
        Guid riderId,
        string tenantId,
        decimal fareAmount,
        string fareCurrency)
    {
        return new CompleteRideSagaState
        {
            SagaId = Guid.NewGuid(),
            RideId = rideId,
            DriverId = driverId,
            RiderId = riderId,
            TenantId = tenantId,
            FareAmount = fareAmount,
            FareCurrency = fareCurrency,
            Status = CompleteRideSagaStatus.Pending,
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void MarkRideCompleted()
    {
        Status = CompleteRideSagaStatus.RideCompleted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkDriverFreed()
    {
        Status = CompleteRideSagaStatus.DriverFreed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPaymentCharged(Guid paymentId)
    {
        PaymentId = paymentId;
        Status = CompleteRideSagaStatus.PaymentCharged;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        Status = CompleteRideSagaStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = CompleteRideSagaStatus.Failed;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFreeDriverFailed(string reason)
    {
        Status = CompleteRideSagaStatus.FreeDriverFailed;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPaymentFailed(string reason)
    {
        Status = CompleteRideSagaStatus.PaymentFailed;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPayoutFailed(string reason)
    {
        Status = CompleteRideSagaStatus.PayoutFailed;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementRetryCount()
    {
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;
    }
}
