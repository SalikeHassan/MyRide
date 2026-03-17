using MyRide.Application.Ports;
using MyRide.Domain.Sagas;

namespace MyRide.Application.Sagas;

public class CompleteRideSaga
{
    private const int MaxRetries = 3;

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
            await ExecuteStep1(saga);
        }

        if (saga.Status == CompleteRideSagaStatus.RideCompleted)
        {
            await ExecuteStep2(saga);
        }

        if (saga.Status == CompleteRideSagaStatus.DriverFreed)
        {
            await ExecuteStep3(saga);
        }

        if (saga.Status == CompleteRideSagaStatus.PaymentCharged)
        {
            await ExecuteStep4(saga);
        }

        return saga;
    }

    private async Task ExecuteStep1(CompleteRideSagaState saga)
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

    private async Task ExecuteStep2(CompleteRideSagaState saga)
    {
        try
        {
            await driversClient.FreeDriver(saga.DriverId, saga.RideId, saga.TenantId);
            saga.MarkDriverFreed();
            await repository.Save(saga);
        }
        catch (Exception ex)
        {
            if (saga.RetryCount >= MaxRetries)
            {
                saga.MarkFreeDriverFailed(ex.Message);
                await repository.Save(saga);
                return;
            }

            saga.IncrementRetryCount();
            await repository.Save(saga);
        }
    }

    private async Task ExecuteStep3(CompleteRideSagaState saga)
    {
        try
        {
            var paymentId = await paymentsClient.ChargeRider(
                saga.RiderId, saga.DriverId,
                saga.FareAmount, saga.FareCurrency,
                saga.TenantId);

            saga.MarkPaymentCharged(paymentId);
            await repository.Save(saga);
        }
        catch (Exception ex)
        {
            if (saga.RetryCount >= MaxRetries)
            {
                saga.MarkPaymentFailed(ex.Message);
                await repository.Save(saga);
                return;
            }

            saga.IncrementRetryCount();
            await repository.Save(saga);
        }
    }

    private async Task ExecuteStep4(CompleteRideSagaState saga)
    {
        try
        {
            await payoutsClient.PayDriver(
                saga.DriverId,
                saga.FareAmount, saga.FareCurrency,
                saga.TenantId);

            saga.MarkCompleted();
            await repository.Save(saga);
        }
        catch (Exception ex)
        {
            if (saga.RetryCount >= MaxRetries)
            {
                saga.MarkPayoutFailed(ex.Message);
                await repository.Save(saga);
                return;
            }

            saga.IncrementRetryCount();
            await repository.Save(saga);
        }
    }
}
