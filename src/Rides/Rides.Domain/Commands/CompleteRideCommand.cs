using Common.Domain;

namespace Rides.Domain.Commands;

public class CompleteRideCommand : ICommand
{
    public Guid RideId { get; }
    public string TenantId { get; }

    public CompleteRideCommand(Guid rideId, string tenantId)
    {
        RideId = rideId;
        TenantId = tenantId;
    }
}
