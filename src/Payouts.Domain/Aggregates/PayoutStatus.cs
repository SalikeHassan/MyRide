namespace Payouts.Domain.Aggregates;

public enum PayoutStatus
{
    Pending,
    Paid,
    Failed,
    Cancelled
}