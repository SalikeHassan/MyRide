using Common.Domain;

namespace Payouts.Application.Ports;

public interface IPayoutEventPublisher
{
    Task Publish(IDomainEvent domainEvent);
}