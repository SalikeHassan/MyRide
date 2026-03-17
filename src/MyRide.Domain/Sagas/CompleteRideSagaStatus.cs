namespace MyRide.Domain.Sagas;

public enum CompleteRideSagaStatus
{
    Pending,
    RideCompleted,
    DriverFreed,
    PaymentCharged,
    Completed,
    Failed,
    FreeDriverFailed,
    PaymentFailed,
    PayoutFailed
}
