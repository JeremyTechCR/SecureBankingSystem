using SecureBanking.Domain.Exceptions;
using SecureBanking.Domain.ValueObjects;

namespace SecureBanking.Domain.UnitTests.ValueObjects;

public sealed class CurrencyAndMoneyTests
{
    public static TheoryData<decimal> ValidMonetaryAmounts => new()
    {
        0m,
        1m,
        1.2m,
        1.23m,
        1.234m,
        1.2345m,
        0.0001m,
        1.23450m
    };

    public static TheoryData<decimal> InvalidMonetaryPrecisions => new()
    {
        0.00001m,
        1.23456m
    };

    [Theory]
    [InlineData("crc", "CRC")]
    [InlineData(" USD ", "USD")]
    public void CurrencyFrom_WithSupportedCode_NormalizesCode(string input, string expected)
    {
        Assert.Equal(expected, Currency.From(input).Code);
    }

    [Fact]
    public void Currency_WithEquivalentCodes_HasValueEquality()
    {
        Currency first = Currency.Usd;
        Currency second = Currency.From("usd");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, Currency.Crc);
        Assert.False(first.Equals("USD"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("EUR")]
    public void CurrencyFrom_WithUnsupportedCode_ThrowsDomainException(string? code)
    {
        Assert.Throws<InvalidDomainArgumentException>(() => Currency.From(code));
    }

    [Fact]
    public void MoneyZero_ReturnsZeroInRequestedCurrency()
    {
        Assert.Equal(new Money(0m, Currency.Crc), Money.Zero(Currency.Crc));
    }

    [Fact]
    public void Money_WithEqualValues_HasValueEquality()
    {
        Money first = new(12.50m, Currency.Usd);
        Money second = new(12.50m, Currency.From("USD"));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, new Money(12.51m, Currency.Usd));
        Assert.NotEqual(first, new Money(12.50m, Currency.Crc));
        Assert.False(first.Equals(12.50m));
    }

    [Fact]
    public void Money_WithNegativeAmount_ThrowsDomainException()
    {
        Assert.Throws<InvalidDomainArgumentException>(() => new Money(-0.01m, Currency.Usd));
    }

    [Fact]
    public void Money_WithNullCurrency_ThrowsInvalidDomainArgument()
    {
        Assert.Throws<InvalidDomainArgumentException>(() => new Money(1m, null!));
    }

    [Fact]
    public void MoneyZero_WithNullCurrency_ThrowsInvalidDomainArgument()
    {
        Assert.Throws<InvalidDomainArgumentException>(() => Money.Zero(null!));
    }

    [Theory]
    [MemberData(nameof(ValidMonetaryAmounts))]
    public void Money_WithAtMostFourSignificantDecimalPlaces_Succeeds(decimal amount)
    {
        Money money = new(amount, Currency.Usd);

        Assert.Equal(amount, money.Amount);
    }

    [Theory]
    [MemberData(nameof(InvalidMonetaryPrecisions))]
    public void Money_WithMoreThanFourDecimalPlaces_ThrowsInvalidMonetaryPrecision(decimal amount)
    {
        Assert.Throws<InvalidMonetaryPrecisionException>(() => new Money(amount, Currency.Usd));
    }

    [Fact]
    public void Money_WithMaximumAmount_Succeeds()
    {
        Money money = new(Money.MaximumAmount, Currency.Usd);

        Assert.Equal(Money.MaximumAmount, money.Amount);
    }

    [Fact]
    public void Money_AboveMaximumAmount_ThrowsMonetaryLimitExceeded()
    {
        Assert.Throws<MonetaryLimitExceededException>(
            () => new Money(Money.MaximumAmount + 0.0001m, Currency.Usd));
    }

    [Fact]
    public void Add_WhenResultExceedsMaximum_ThrowsDomainExceptionInsteadOfOverflowException()
    {
        Exception exception = Assert.Throws<MonetaryLimitExceededException>(
            () => new Money(Money.MaximumAmount, Currency.Usd).Add(new Money(0.0001m, Currency.Usd)));

        Assert.IsNotType<OverflowException>(exception);
    }

    [Fact]
    public void Add_WithFourDecimalPlaces_PreservesExactAmountWithoutRounding()
    {
        Money result = new Money(1.2344m, Currency.Usd).Add(new Money(0.0001m, Currency.Usd));

        Assert.Equal(1.2345m, result.Amount);
    }

    [Fact]
    public void Subtract_WithFourDecimalPlaces_PreservesExactAmountWithoutRounding()
    {
        Money result = new Money(1.2345m, Currency.Usd).Subtract(new Money(0.0001m, Currency.Usd));

        Assert.Equal(1.2344m, result.Amount);
    }

    [Fact]
    public void Add_WithSameCurrency_ReturnsSum()
    {
        Money result = new Money(10m, Currency.Usd).Add(new Money(2.50m, Currency.Usd));

        Assert.Equal(new Money(12.50m, Currency.Usd), result);
    }

    [Fact]
    public void Subtract_WithSameCurrencyAndAvailableFunds_ReturnsDifference()
    {
        Money result = new Money(10m, Currency.Crc).Subtract(new Money(4m, Currency.Crc));

        Assert.Equal(new Money(6m, Currency.Crc), result);
    }

    [Fact]
    public void Add_WithDifferentCurrency_ThrowsCurrencyMismatch()
    {
        Assert.Throws<CurrencyMismatchException>(
            () => new Money(10m, Currency.Usd).Add(new Money(1m, Currency.Crc)));
    }

    [Fact]
    public void Subtract_WithoutEnoughFunds_ThrowsInsufficientFunds()
    {
        Assert.Throws<InsufficientFundsException>(
            () => new Money(1m, Currency.Usd).Subtract(new Money(2m, Currency.Usd)));
    }
}
