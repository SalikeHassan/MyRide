using SharedKernel;

namespace Rides.Domain.ValueObjects;

public class Fare : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Fare(decimal amount, string currency)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Fare amount must be greater than zero.", nameof(amount));
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
