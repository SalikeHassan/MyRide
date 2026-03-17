using Drivers.Domain.Entities;

namespace Drivers.Domain.Ports;

public interface IDriverRepository
{
    Task<Driver?> GetAvailable(string tenantId);
    Task<Driver?> GetById(Guid id, string tenantId);
    Task UpdateStatus(Guid id, DriverStatus status, string tenantId);
}
