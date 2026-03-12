using Payouts.Domain.Commands;
using Payouts.Domain.Events;
using Payouts.Domain.ValueObjects;
using SharedKernel;

namespace Payouts.Domain.Aggregates;

public class PayoutAggregate : AggregateRoot
{
    public Guid RecipientId { get; private set; }
    public Disbursement Disbursement { get; private set; }
    public PayoutStatus PayoutStatus { get; private set; }

    private PayoutAggregate()
    {
        
    }
    
    public static PayoutAggregate Load(IEnumerable<IDomainEvent> events)
    {
        var payout = new PayoutAggregate();
        payout.Rehydrate(events);
        return payout;
    }

    public static PayoutAggregate Pay(PayDriverCommand command)
    {
        if (command.RecipientId == Guid.Empty)
        {
            throw new ArgumentException("RecipientId cannot be empty", nameof(command.RecipientId));
        }
        
        var disbursement = new Disbursement(command.Amount,command.Currency);

        var payout = new PayoutAggregate();

        if (command.SimulateFailure)
        {
            payout.RaiseEvent(new DriverPayFailed(
                command.TenantId,
                command.PayoutId,
                command.RecipientId,
                "Simulate Failure"
                ));
        }

        else
        {
            payout.RaiseEvent(new DriverPaid(
                command.TenantId,
                command.PayoutId,
                command.RecipientId,
                disbursement.Amount,
                disbursement.Currency));
        }
        
        return payout;
    }

    public void Cancel(string reason)
    {
        if (PayoutStatus == PayoutStatus.Paid)
        {
            throw new InvalidOperationException("Cannot cancel completed payout");
        }

        if (PayoutStatus == PayoutStatus.Cancelled)
        {
            throw new InvalidOperationException("Payout is already cancelled");
        }
        
        RaiseEvent(new DriverPayFailed(TenantId,Id,RecipientId,reason));
    }

    private void Apply(DriverPaid @event)
    {
        Id =  @event.PayoutId;
        TenantId = @event.TenantId;
        RecipientId = @event.RecipientId;
        Disbursement = new Disbursement(@event.Amount, @event.Currency);
        PayoutStatus = PayoutStatus.Paid;
    }
    
    private void Apply(DriverPayFailed @event)
    {
        Id = @event.PayoutId;
        TenantId = @event.TenantId;
        RecipientId = @event.RecipientId;
        PayoutStatus = PayoutStatus.Cancelled;
    }
    
    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case DriverPayFailed @event:
                Apply(@event);
                break;
            case DriverPaid @event:
                Apply(@event);
                break;
            default:
                throw new InvalidOperationException($"Unknown domain event {domainEvent.GetType().Name}");
        }
    }
}