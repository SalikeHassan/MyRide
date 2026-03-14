using Microsoft.EntityFrameworkCore;
using Rides.Application.Ports;
using Rides.Domain.Aggregates;

namespace ReadDb.Infrastructure.Repositories;

public class SqlRideReadStore : IRideReadStore
{
    private readonly ReadDbContext context;

    public SqlRideReadStore(ReadDbContext context)
    {
        this.context = context;
    }

    public async Task<bool> HasActiveRideForRiderAsync(Guid riderId, string tenantId)
    {
        var activeStatuses = new[] { RideStatus.Requested, RideStatus.InProgress };

        return await context.RideReadModels
            .AnyAsync(r => r.RiderId == riderId
                        && r.TenantId == tenantId
                        && activeStatuses.Contains(r.Status));
    }

    public async Task<bool> HasActiveRideForDriverAsync(Guid driverId, string tenantId)
    {
        var activeStatuses = new[] { RideStatus.Requested, RideStatus.InProgress };

        return await context.RideReadModels
            .AnyAsync(r => r.DriverId == driverId
                        && r.TenantId == tenantId
                        && activeStatuses.Contains(r.Status));
    }

    public async Task UpsertAsync(RideReadModel readModel)
    {
        var existing = await context.RideReadModels
            .FirstOrDefaultAsync(r => r.RideId == readModel.RideId);

        if (existing is null)
        {
            context.RideReadModels.Add(readModel);
        }
        else
        {
            existing.Status = readModel.Status;
            existing.LastUpdatedOn = readModel.LastUpdatedOn;
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<RideReadModel>> GetActiveRidesAsync(string tenantId)
    {
        var visibleStatuses = new[] { RideStatus.Requested, RideStatus.InProgress, RideStatus.Completed };

        return await context.RideReadModels
            .Where(r => r.TenantId == tenantId && visibleStatuses.Contains(r.Status))
            .OrderByDescending(r => r.LastUpdatedOn)
            .ToListAsync();
    }
}
