using SecureBanking.Domain.Exceptions;
using SecureBanking.Domain.ValueObjects;

namespace SecureBanking.Domain.UnitTests.ValueObjects;

public sealed class CurrencyAndMoneyTests
{
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
        Assert.Equal(Currency.Usd, Currency.From("usd"));
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
        Assert.Equal(new Money(12.50m, Currency.Usd), new Money(12.50m, Currency.From("USD")));
    }

    [Fact]
    public void Money_WithNegativeAmount_ThrowsDomainException()
    {
        Assert.Throws<InvalidDomainArgumentException>(() => new Money(-0.01m, Currency.Usd));
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
