using Drivers.Domain.Aggregates;

namespace Drivers.Application.Ports;

public interface IDriverEventStore
{
    Task Append(DriverAggregate driver);
    Task<DriverAggregate> Load(Guid driverId, string tenantId);
    Task<bool> Exists(Guid driverId, string tenantId);
}
