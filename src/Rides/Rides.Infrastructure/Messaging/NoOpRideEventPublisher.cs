using Rides.Application.Ports;
using Common.Domain;

namespace Rides.Infrastructure.Messaging;

public class NoOpRideEventPublisher : IRideEventPublisher
{
    public Task Publish(IDomainEvent domainEvent)
    {
        return Task.CompletedTask;
    }
}