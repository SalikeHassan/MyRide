using SharedKernel;

namespace Payments.Application.Ports;

public interface IPaymentEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent);
}