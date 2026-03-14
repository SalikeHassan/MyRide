using Rides.Application.Ports;

namespace Rides.Application.Handlers;

public class GetActiveRidesHandler
{
    private readonly IRideReadStore rideReadStore;

    public GetActiveRidesHandler(IRideReadStore rideReadStore)
    {
        this.rideReadStore = rideReadStore;
    }

    public async Task<List<RideReadModel>> HandleAsync(string tenantId)
    {
        return await rideReadStore.GetActiveRidesAsync(tenantId);
    }
}
