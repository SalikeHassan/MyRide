using Drivers.Domain.Entities;
using Drivers.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace ReadDb.Infrastructure.Repositories;

public class SqlDriverRepository : IDriverRepository
{
    private readonly ReadDbContext context;

    public SqlDriverRepository(ReadDbContext context)
    {
        this.context = context;
    }

    public async Task<Driver?> GetAvailableAsync(string tenantId)
    {
        return await context.Drivers
            .FirstOrDefaultAsync(d => d.TenantId == tenantId
                                   && d.Status == DriverStatus.Available);
    }

    public async Task<List<Driver>> GetAllAsync(string tenantId)
    {
        return await context.Drivers
            .Where(d => d.TenantId == tenantId)
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<Driver?> GetByIdAsync(Guid id, string tenantId)
    {
        return await context.Drivers
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId);
    }

    public async Task UpdateStatusAsync(Guid id, DriverStatus status, string tenantId)
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
