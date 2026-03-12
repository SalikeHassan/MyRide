using Payments.Domain.Commands;
using Payments.Domain.Events;
using Payments.Domain.ValueObjects;
using SharedKernel;

namespace Payments.Domain.Aggregate;

public class PaymentAggregate : AggregateRoot
{
    public Guid PayerId { get; private set; }
    public Guid PayeeId { get; private set; }

    public ChargeAmount? ChargeAmount { get; private set; } = null;
    public PaymentStatus Status { get; private set; }

    private PaymentAggregate(){}

    public static PaymentAggregate Load(IEnumerable<IDomainEvent> domainEvents)
    {
        var payment = new PaymentAggregate();
        payment.Rehydrate(domainEvents);
        
        return payment;
    }

    public static PaymentAggregate Charge(ChargeRiderCommand command)
    {
        if (command.PayeeId == Guid.Empty)
        {
            throw new ArgumentException("Payee id cannot be empty");
        }

        if (command.PayeeId == Guid.Empty)
        {
            throw new ArgumentException("Payee id cannot be empty");
        }
        
        var chargeAmount = new ChargeAmount(command.Amount, command.Currency);

        var payment = new PaymentAggregate();

        if (command.SimulateFailure)
        {
            payment.RaiseEvent(new RiderChargeFailed(
                command.TenantId,
                command.PaymentId,
                command.PayerId, 
                "Payment failed"));
        }

        else
        {
            payment.RaiseEvent(new RiderCharged(
                command.TenantId,
                command.PaymentId,
                command.PayerId,
                command.PayeeId,
                chargeAmount.Amount,
                chargeAmount.Currency));
        }
        
        return payment;
    }

    public void Refund()
    {
        if (Status == PaymentStatus.Pending)
        {
            throw new InvalidOperationException("Cannot refund a payment that has not been charged.");
        }

        if (Status == PaymentStatus.ChargeFailed)
        {
            throw new InvalidOperationException("Cannot refund a failed charge.");
        }

        if (Status == PaymentStatus.Refunded)
        {
            throw new InvalidOperationException("Payment has already been refunded.");
        }
        
        RaiseEvent(new RiderRefunded(TenantId, Id, PayerId, ChargeAmount.Amount, ChargeAmount.Currency));
    }

    private void Apply(RiderRefunded @event)
    {
        Status = PaymentStatus.Refunded;
    }

    private void Apply(RiderCharged @event)
    {
        Id = @event.PaymentId;
        TenantId = @event.TenantId;
        PayeeId = @event.PayeeId;
        PayeeId = @event.PayeeId;
        ChargeAmount = new ChargeAmount(@event.Amount, @event.Currency);
        Status = PaymentStatus.Charged;
    }

    private void Apply(RiderChargeFailed @event)
    {
        Id = @event.PaymentId;
        TenantId = @event.TenantId;
        PayerId = @event.PayerId;
        Status = PaymentStatus.ChargeFailed;
    }
    
    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case RiderCharged @event:
                Apply(@event);
                break;
            case RiderChargeFailed @event:
                Apply(@event);
                break;
            case RiderRefunded @event:  
                Apply(@event);
                break;
            default:
                throw new InvalidOperationException($"Unknown event {domainEvent.GetType().Name}");
        }
    }
}