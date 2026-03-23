namespace MyRide.Domain.Sagas;

public enum RequestRideSagaStatus
{
    Pending,
    DriverAssigned,
    Completed,
    Failed,
    Compensating,
    Compensated,
    CompensationFailed
}
