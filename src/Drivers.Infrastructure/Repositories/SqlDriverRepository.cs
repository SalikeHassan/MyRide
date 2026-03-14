using Drivers.Domain.Entities;
using Drivers.Domain.Ports;
using Microsoft.EntityFrameworkCore;
using Rides.Domain.Aggregates;

namespace Drivers.Infrastructure.Repositories;

public class SqlDriverRepository : IDriverRepository
{
    private readonly ReadDbContext context;

    public SqlDriverRepository(ReadDbContext context)
    {
        this.context = context;
    }

    public async Task<Driver?> GetAvailableAsync(string tenantId)
    {
        var activeStatuses = new[] { RideStatus.Requested, RideStatus.InProgress };

        var driverIdsWithActiveRide = context.RideReadModels
            .Where(r => r.TenantId == tenantId && activeStatuses.Contains(r.Status))
            .Select(r => r.DriverId);

        return await context.Drivers
            .Where(d => d.TenantId == tenantId
                     && !driverIdsWithActiveRide.Contains(d.Id))
            .FirstOrDefaultAsync();
    }
}
