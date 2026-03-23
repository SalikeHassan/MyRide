using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyRide.Application.Ports;
using MyRide.Application.Sagas;
using MyRide.Domain.Sagas;

namespace MyRide.Application.Recovery;

public class SagaRecoveryJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);
    private readonly IServiceScopeFactory scopeFactory;

    public SagaRecoveryJob(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken);
            await RecoverRequestRideSagas();
            await RecoverCompleteRideSagas();
        }
    }

    private async Task RecoverRequestRideSagas()
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRequestRideSagaRepository>();
        var saga = scope.ServiceProvider.GetRequiredService<IRequestRideSaga>();

        var stuck = await repository.GetStuck();

        foreach (var state in stuck)
        {
            if (state.Status == RequestRideSagaStatus.Compensating)
            {
                await saga.Compensate(state);
            }
        }
    }

    private async Task RecoverCompleteRideSagas()
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICompleteRideSagaRepository>();
        var saga = scope.ServiceProvider.GetRequiredService<ICompleteRideSaga>();

        var stuck = await repository.GetStuck();

        foreach (var state in stuck)
        {
            await saga.Resume(state);
        }
    }
}
