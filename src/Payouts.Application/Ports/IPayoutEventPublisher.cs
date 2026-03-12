using SharedKernel;

namespace Payouts.Application.Ports;

public interface IPayoutEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent);
}