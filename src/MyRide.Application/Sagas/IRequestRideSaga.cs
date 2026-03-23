using MyRide.Domain.Sagas;

namespace MyRide.Application.Sagas;

public interface IRequestRideSaga
{
    Task<RequestRideSagaState> Execute(
        Guid driverId,
        Guid riderId,
        string driverName,
        string tenantId,
        decimal fareAmount,
        string fareCurrency,
        double pickupLat,
        double pickupLng,
        double dropoffLat,
        double dropoffLng);

    Task Compensate(RequestRideSagaState saga);
}
