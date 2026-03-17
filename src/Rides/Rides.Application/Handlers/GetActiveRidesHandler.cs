using Common.Application;
using Rides.Application.Ports;
using Rides.Application.Queries;
using Rides.Domain.ReadModels;

namespace Rides.Application.Handlers;

public class GetActiveRidesHandler : IQueryHandler<GetActiveRidesQuery, List<RideReadModel>>
{
    private readonly IRideReadStore rideReadStore;

    public GetActiveRidesHandler(IRideReadStore rideReadStore)
    {
        this.rideReadStore = rideReadStore;
    }

    public async Task<List<RideReadModel>> Handle(GetActiveRidesQuery query)
    {
        return await rideReadStore.GetActiveRides(query.TenantId);
    }

    public async Task<RideReadModel?> GetById(Guid rideId, string tenantId)
    {
        return await rideReadStore.GetById(rideId, tenantId);
    }
}
