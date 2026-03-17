using Common.Domain;

namespace Drivers.Domain.Commands;

public record FreeDriverCommand(
    Guid DriverId,
    Guid RideId,
    string TenantId) : ICommand;
