using Common.Domain;

namespace Rides.Domain.Commands;

public record AcceptRideCommand(Guid RideId, string TenantId) : ICommand;
