using SharedKernel;

namespace Rides.Application.Ports;

public interface IRideEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent);
}
