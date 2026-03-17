namespace MyRide.Domain.Sagas;

public enum StartRideSagaStatus
{
    Pending,
    DriverAssigned,
    Completed,
    Failed,
    Compensating,
    Compensated,
    CompensationFailed
}
