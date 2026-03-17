using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Rides.Application.Ports;
using Common.Domain;

namespace Rides.Infrastructure.Messaging;

public class RideEventPublisher : IRideEventPublisher
{
    private readonly ServiceBusSender sender;

    public RideEventPublisher([FromKeyedServices("rides")] ServiceBusSender sender)
    {
        this.sender = sender;
    }

    public async Task Publish(IDomainEvent domainEvent)
    {
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

        var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(payload))
        {
            Subject = domainEvent.GetType().Name,
            ApplicationProperties =
            {
                ["EventType"] = domainEvent.GetType().Name,
                ["TenantId"] = domainEvent.TenantId
            }
        };

        await sender.SendMessageAsync(message);
    }
}
