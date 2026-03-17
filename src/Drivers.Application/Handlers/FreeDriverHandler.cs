using Common.Application;
using Drivers.Application.Ports;
using Drivers.Domain.Commands;
using Drivers.Domain.Entities;
using Drivers.Domain.Ports;

namespace Drivers.Application.Handlers;

public class FreeDriverHandler : ICommandHandler<FreeDriverCommand>
{
    private readonly IDriverEventStore driverEventStore;
    private readonly IDriverRepository driverRepository;

    public FreeDriverHandler(IDriverEventStore driverEventStore, IDriverRepository driverRepository)
    {
        this.driverEventStore = driverEventStore;
        this.driverRepository = driverRepository;
    }

    public async Task Handle(FreeDriverCommand command)
    {
        var driver = await driverEventStore.Load(command.DriverId, command.TenantId);

        driver.Free(command.RideId);

        if (driver.DomainEvents.Count == 0)
        {
            return;
        }

        await driverEventStore.Append(driver);

        await driverRepository.UpdateStatus(command.DriverId, DriverStatus.Available, command.TenantId);
    }
}
