namespace Rides.Domain.Commands;

public class CancelRideCommand
{
    public Guid RideId { get; }
    public string TenantId { get; }
    public string Reason { get; }

    public CancelRideCommand(Guid rideId, string tenantId, string reason)
    {
        RideId = rideId;
        TenantId = tenantId;
        Reason = reason;
    }
}
