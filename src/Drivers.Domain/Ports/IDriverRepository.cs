using Drivers.Domain.Entities;

namespace Drivers.Domain.Ports;

public interface IDriverRepository
{
    Task<Driver?> GetAvailableAsync(string tenantId);
}
