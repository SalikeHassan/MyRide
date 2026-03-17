using Common.Domain;

namespace Drivers.Domain.Commands;

public record AssignDriverCommand(
    Guid DriverId,
    Guid RideId,
    string TenantId) : ICommand;
