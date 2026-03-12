using SharedKernel;

namespace Payouts.Domain.ValueObjects;

public class Disbursement : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Disbursement(decimal amount, string currency)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Disbursement amount must be greater than zero.", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency must not be empty.", nameof(currency));
        }

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}