using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Payouts.Application.Ports;
using Common.Domain;

namespace Payouts.Infrastructure.Messaging;

public class PayoutEventPublisher : IPayoutEventPublisher
{
    private readonly ServiceBusSender sender;

    public PayoutEventPublisher([FromKeyedServices("payouts")] ServiceBusSender sender)
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