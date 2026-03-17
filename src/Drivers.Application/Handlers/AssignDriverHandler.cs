using Common.Application;
using Drivers.Application.Ports;
using Drivers.Domain.Commands;
using Drivers.Domain.Entities;
using Drivers.Domain.Ports;

namespace Drivers.Application.Handlers;

public class AssignDriverHandler : ICommandHandler<AssignDriverCommand>
{
    private readonly IDriverEventStore driverEventStore;
    private readonly IDriverRepository driverRepository;

    public AssignDriverHandler(IDriverEventStore driverEventStore, IDriverRepository driverRepository)
    {
        this.driverEventStore = driverEventStore;
        this.driverRepository = driverRepository;
    }

    public async Task Handle(AssignDriverCommand command)
    {
        var driver = await driverEventStore.Load(command.DriverId, command.TenantId);

        driver.Assign(command.RideId);

        await driverEventStore.Append(driver);

        await driverRepository.UpdateStatus(command.DriverId, DriverStatus.InProgress, command.TenantId);
    }
}
