using Microsoft.EntityFrameworkCore;
using Rides.Application.Ports;
using Rides.Domain.Aggregates;
using Rides.Domain.ReadModels;

namespace Rides.Infrastructure.Persistence;

public class SqlRideReadStore : IRideReadStore
{
    private readonly RidesReadDbContext context;

    public SqlRideReadStore(RidesReadDbContext context)
    {
        this.context = context;
    }

    public async Task<bool> HasActiveRideForRider(Guid riderId, string tenantId)
    {
        var activeStatuses = new[] { RideStatus.Requested, RideStatus.InProgress };

        return await context.RideReadModels
            .AnyAsync(r => r.RiderId == riderId
                        && r.TenantId == tenantId
                        && activeStatuses.Contains(r.Status));
    }

    public async Task<RideReadModel?> GetById(Guid rideId, string tenantId)
    {
        return await context.RideReadModels
            .FirstOrDefaultAsync(r => r.RideId == rideId && r.TenantId == tenantId);
    }

    public async Task Upsert(RideReadModel readModel)
    {
        var existing = await context.RideReadModels.FindAsync(readModel.RideId);

        if (existing is null)
        {
            context.RideReadModels.Add(readModel);
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<RideReadModel>> GetActiveRides(string tenantId)
    {
        var visibleStatuses = new[] { RideStatus.Requested, RideStatus.InProgress, RideStatus.Completed };

        return await context.RideReadModels
            .Where(r => r.TenantId == tenantId && visibleStatuses.Contains(r.Status))
            .OrderByDescending(r => r.LastUpdatedOn)
            .ToListAsync();
    }
}
