using Drivers.Application.Handlers;
using Drivers.Domain.Commands;
using Drivers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Drivers.Infrastructure.Services;

public class DriverSeedService : IHostedService
{
    private readonly IServiceScopeFactory scopeFactory;

    public DriverSeedService(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<DriversReadDbContext>();
        var onboardHandler = scope.ServiceProvider.GetRequiredService<OnboardDriverHandler>();

        var drivers = await dbContext.Drivers.ToListAsync(cancellationToken);

        foreach (var driver in drivers)
        {
            await onboardHandler.Handle(new OnboardDriverCommand(
                driver.Id,
                driver.TenantId,
                driver.Name,
                driver.Phone));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
