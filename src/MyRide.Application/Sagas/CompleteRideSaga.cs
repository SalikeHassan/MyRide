using MyRide.Application.Ports;
using MyRide.Domain.Sagas;

namespace MyRide.Application.Sagas;

public class CompleteRideSaga : ICompleteRideSaga
{
    private readonly ICompleteRideSagaRepository repository;
    private readonly IDownstreamRidesClient ridesClient;
    private readonly IDownstreamDriversClient driversClient;
    private readonly IDownstreamPaymentsClient paymentsClient;
    private readonly IDownstreamPayoutsClient payoutsClient;

    public CompleteRideSaga(
        ICompleteRideSagaRepository repository,
        IDownstreamRidesClient ridesClient,
        IDownstreamDriversClient driversClient,
        IDownstreamPaymentsClient paymentsClient,
        IDownstreamPayoutsClient payoutsClient)
    {
        this.repository = repository;
        this.ridesClient = ridesClient;
        this.driversClient = driversClient;
        this.paymentsClient = paymentsClient;
        this.payoutsClient = payoutsClient;
    }

    public async Task<CompleteRideSagaState> Execute(Guid rideId, string tenantId)
    {
        var ride = await ridesClient.GetRide(rideId, tenantId)
            ?? throw new InvalidOperationException($"Ride {rideId} not found.");

        var saga = CompleteRideSagaState.Create(
            rideId, ride.DriverId, ride.RiderId, tenantId,
            ride.FareAmount, ride.FareCurrency);

        await repository.Save(saga);

        return await Resume(saga);
    }

    public async Task<CompleteRideSagaState> Resume(CompleteRideSagaState saga)
    {
        if (saga.Status == CompleteRideSagaStatus.Pending)
        {
            await CompleteRide(saga);
        }

        if (saga.Status == CompleteRideSagaStatus.RideCompleted
            || saga.Status == CompleteRideSagaStatus.FreeDriverFailed)
        {
            await FreeDriver(saga);
        }

        if (saga.Status == CompleteRideSagaStatus.DriverFreed
            || saga.Status == CompleteRideSagaStatus.PaymentFailed)
        {
            await ChargeRider(saga);
        }

        if (saga.Status == CompleteRideSagaStatus.PaymentCharged
            || saga.Status == CompleteRideSagaStatus.PayoutFailed)
        {
            await PayDriver(saga);
        }

        return saga;
    }

    private async Task CompleteRide(CompleteRideSagaState saga)
    {
        try
        {
            await ridesClient.CompleteRide(saga.RideId, saga.TenantId);
            saga.MarkRideCompleted();
            await repository.Save(saga);
        }
        catch (Exception ex)
        {
            saga.MarkFailed(ex.Message);
            await repository.Save(saga);
        }
    }

    private async Task FreeDriver(CompleteRideSagaState saga)
    {
        try
        {
            await driversClient.FreeDriver(saga.DriverId, saga.RideId, saga.TenantId);
            saga.MarkDriverFreed();
            await repository.Save(saga);
        }
        catch (Exception ex)
        {
            saga.IncrementRetryCount();

            saga.MarkFreeDriverFailed(ex.Message);

            await repository.Save(saga);
        }
    }

    private async Task ChargeRider(CompleteRideSagaState saga)
    {
        try
        {
            var paymentId = await paymentsClient.ChargeRider(
                saga.RideId, saga.RiderId, saga.DriverId,
                saga.FareAmount, saga.FareCurrency,
                saga.TenantId);

            saga.MarkPaymentCharged(paymentId);
            await repository.Save(saga);
        }
        catch (Exception ex)
        {
            saga.IncrementRetryCount();

            saga.MarkPaymentFailed(ex.Message);

            await repository.Save(saga);
        }
    }

    private async Task PayDriver(CompleteRideSagaState saga)
    {
        try
        {
            await payoutsClient.PayDriver(
                saga.RideId, saga.DriverId,
                saga.FareAmount, saga.FareCurrency,
                saga.TenantId);

            saga.MarkCompleted();
            await repository.Save(saga);
        }
        catch (Exception ex)
        {
            saga.IncrementRetryCount();

            saga.MarkPayoutFailed(ex.Message);

            await repository.Save(saga);
        }
    }
}
