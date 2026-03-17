using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyRide.Application.Ports;
using MyRide.Application.Sagas;
using MyRide.Domain.Sagas;

namespace MyRide.Application.Recovery;

public class SagaRecoveryJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);
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
            await RecoverStartRideSagas();
            await RecoverCompleteRideSagas();
        }
    }

    private async Task RecoverStartRideSagas()
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IStartRideSagaRepository>();
        var saga = scope.ServiceProvider.GetRequiredService<StartRideSaga>();

        var stuck = await repository.GetStuck();

        foreach (var state in stuck)
        {
            if (state.Status == StartRideSagaStatus.Compensating)
            {
                await saga.Compensate(state);
            }
        }
    }

    private async Task RecoverCompleteRideSagas()
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICompleteRideSagaRepository>();
        var saga = scope.ServiceProvider.GetRequiredService<CompleteRideSaga>();

        var stuck = await repository.GetStuck();

        foreach (var state in stuck)
        {
            await saga.Resume(state);
        }
    }
}
