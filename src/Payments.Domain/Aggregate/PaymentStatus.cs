namespace Payments.Domain.Aggregate;

public enum PaymentStatus
{
    Pending,
    Charged,
    ChargeFailed,
    Refunded
}