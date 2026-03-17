using Common.Domain;

namespace Rides.Application.Ports;

public interface IRideEventPublisher
{
    Task Publish(IDomainEvent domainEvent);
}
