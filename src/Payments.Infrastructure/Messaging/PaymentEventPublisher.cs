using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Payments.Application.Ports;
using SharedKernel;

namespace Payments.Infrastructure.Messaging;

public class PaymentEventPublisher : IPaymentEventPublisher
{
    private readonly ServiceBusSender sender;

    public PaymentEventPublisher([FromKeyedServices("payments")]
        ServiceBusSender sender)
    {
        this.sender = sender;
    }

    public async Task PublishAsync(IDomainEvent domainEvent)
    {
        var payload = JsonSerializer.Serialize(domainEvent,
            domainEvent.GetType());

        var message = new
            ServiceBusMessage(Encoding.UTF8.GetBytes(payload))
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
