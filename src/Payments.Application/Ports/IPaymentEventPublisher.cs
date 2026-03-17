using Common.Domain;

namespace Payments.Application.Ports;

public interface IPaymentEventPublisher
{
    Task Publish(IDomainEvent domainEvent);
}