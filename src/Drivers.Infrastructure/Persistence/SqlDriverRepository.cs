using Drivers.Domain.Entities;
using Drivers.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Drivers.Infrastructure.Persistence;

public class SqlDriverRepository : IDriverRepository
{
    private readonly DriversReadDbContext context;

    public SqlDriverRepository(DriversReadDbContext context)
    {
        this.context = context;
    }

    public async Task<Driver?> GetAvailable(string tenantId)
    {
        return await context.Drivers
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Status == DriverStatus.Available);
    }

    public async Task<Driver?> GetById(Guid id, string tenantId)
    {
        return await context.Drivers
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId);
    }

    public async Task UpdateStatus(Guid id, DriverStatus status, string tenantId)
    {
        var driver = await context.Drivers
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId);

        if (driver is null)
        {
            return;
        }

        if (status == DriverStatus.Available)
        {
            driver.MakeAvailable();
        }
        else
        {
            driver.MakeInProgress();
        }

        await context.SaveChangesAsync();
    }
}
