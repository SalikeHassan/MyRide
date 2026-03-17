using Common.Application;
using Drivers.Application.Ports;
using Drivers.Domain.Aggregates;
using Drivers.Domain.Commands;

namespace Drivers.Application.Handlers;

public class OnboardDriverHandler : ICommandHandler<OnboardDriverCommand>
{
    private readonly IDriverEventStore driverEventStore;

    public OnboardDriverHandler(IDriverEventStore driverEventStore)
    {
        this.driverEventStore = driverEventStore;
    }

    public async Task Handle(OnboardDriverCommand command)
    {
        if (await driverEventStore.Exists(command.DriverId, command.TenantId))
        {
            return;
        }

        var driver = DriverAggregate.Onboard(command);

        await driverEventStore.Append(driver);
    }
}
