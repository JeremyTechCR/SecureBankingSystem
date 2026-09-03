using SecureBanking.Domain.Exceptions;

namespace SecureBanking.Domain.ValueObjects;

public sealed record Money
{
    public const decimal MaximumAmount = 999999999999999.9999m;

    public Money(decimal amount, Currency currency)
    {
        if (amount < 0m)
        {
            throw new InvalidDomainArgumentException("Money amount cannot be negative.");
        }

        if (amount > MaximumAmount)
        {
            throw new MonetaryLimitExceededException($"Money amount cannot exceed {MaximumAmount}.");
        }

        decimal scaledAmount = amount * 10000m;
        if (scaledAmount != decimal.Truncate(scaledAmount))
        {
            throw new InvalidMonetaryPrecisionException("Money amount cannot have more than four decimal places.");
        }

        Currency = currency ?? throw new InvalidDomainArgumentException("Currency is required.");
        Amount = amount;
    }

    public decimal Amount { get; }

    public Currency Currency { get; }

    public static Money Zero(Currency currency) => new(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);

        try
        {
            decimal result = checked(Amount + other.Amount);
            return new Money(result, Currency);
        }
        catch (OverflowException exception)
        {
            throw new MonetaryLimitExceededException("The sum exceeds the supported monetary limit.", exception);
        }
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        if (other.Amount > Amount)
        {
            throw new InsufficientFundsException("The subtraction would produce a negative balance.");
        }

        return new Money(Amount - other.Amount, Currency);
    }

    private void EnsureSameCurrency(Money? other)
    {
        if (other is null || Currency != other.Currency)
        {
            throw new CurrencyMismatchException("Money values must use the same currency.");
        }
    }
}
