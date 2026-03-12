using Rides.Application.Ports;
using SharedKernel;

namespace Rides.Infrastructure.Messaging;

public class NoOpRideEventPublisher : IRideEventPublisher
{
    public Task PublishAsync(IDomainEvent domainEvent)
    {
        return Task.CompletedTask;
    }
}
