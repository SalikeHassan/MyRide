using SharedKernel;

namespace Payments.Domain.ValueObjects;

public class ChargeAmount : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    public ChargeAmount(decimal amount, string currency)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency must be specified", nameof(currency));
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