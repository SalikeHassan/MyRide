using MyRide.Application.Models;
using MyRide.Application.Ports;
using MyRide.Domain.Sagas;

namespace MyRide.Application.Sagas;

public class StartRideSaga
{
    private readonly IStartRideSagaRepository repository;
    private readonly IDownstreamDriversClient driversClient;
    private readonly IDownstreamRidesClient ridesClient;

    public StartRideSaga(
        IStartRideSagaRepository repository,
        IDownstreamDriversClient driversClient,
        IDownstreamRidesClient ridesClient)
    {
        this.repository = repository;
        this.driversClient = driversClient;
        this.ridesClient = ridesClient;
    }

    public async Task<StartRideSagaState> Execute(
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
        var rideId = Guid.NewGuid();

        var saga = StartRideSagaState.Create(
            rideId, driverId, riderId, driverName, tenantId,
            fareAmount, fareCurrency,
            pickupLat, pickupLng, dropoffLat, dropoffLng);

        await repository.Save(saga);

        // Step 1: assign driver
        try
        {
            await driversClient.AssignDriver(driverId, rideId, tenantId);
            saga.MarkDriverAssigned();
            await repository.Save(saga);
        }
        catch (Exception ex)
        {
            saga.MarkFailed(ex.Message);
            await repository.Save(saga);
            return saga;
        }

        // Step 2: create ride
        try
        {
            var data = new StartRideData(
                rideId, riderId, driverId, driverName,
                fareAmount, fareCurrency,
                pickupLat, pickupLng, dropoffLat, dropoffLng);

            await ridesClient.StartRide(data, tenantId);
            saga.MarkCompleted();
            await repository.Save(saga);
        }
        catch (Exception ex)
        {
            saga.MarkCompensating(ex.Message);
            await repository.Save(saga);
            await Compensate(saga);
        }

        return saga;
    }

    public async Task Compensate(StartRideSagaState saga)
    {
        try
        {
            await driversClient.FreeDriver(saga.DriverId, saga.RideId, saga.TenantId);
            saga.MarkCompensated();
            await repository.Save(saga);
        }
        catch (Exception ex)
        {
            saga.MarkCompensationFailed(ex.Message);
            await repository.Save(saga);
        }
    }
}
