using Common.Domain;

namespace Drivers.Domain.Commands;

public record OnboardDriverCommand(
    Guid DriverId,
    string TenantId,
    string Name,
    string Phone) : ICommand;
