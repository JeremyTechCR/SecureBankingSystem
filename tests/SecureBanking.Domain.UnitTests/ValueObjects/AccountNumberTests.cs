using SecureBanking.Domain.Exceptions;
using SecureBanking.Domain.ValueObjects;

namespace SecureBanking.Domain.UnitTests.ValueObjects;

public sealed class AccountNumberTests
{
    [Fact]
    public void Create_WithValidValue_PreservesLeadingZeros()
    {
        AccountNumber number = AccountNumber.Create("001234567890");

        Assert.Equal("001234567890", number.GetFullValue());
    }

    [Fact]
    public void ToMaskedString_ShowsOnlyLastFourDigits()
    {
        AccountNumber number = AccountNumber.Create("001234567890");

        Assert.Equal("********7890", number.ToMaskedString());
        Assert.Equal("********7890", number.ToString());
        Assert.DoesNotContain(number.GetFullValue(), number.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void EqualValues_AreEqual()
    {
        AccountNumber first = AccountNumber.Create("001234567890");
        AccountNumber second = AccountNumber.Create("001234567890");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void DifferentValuesAndTypes_AreNotEqual()
    {
        AccountNumber number = AccountNumber.Create("001234567890");

        Assert.NotEqual(number, AccountNumber.Create("001234567891"));
        Assert.False(number.Equals("001234567890"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345678901")]
    [InlineData("1234567890123")]
    [InlineData("12345678901A")]
    [InlineData("1234 6789012")]
    public void Create_WithInvalidFormat_ThrowsDomainException(string? value)
    {
        Assert.Throws<InvalidDomainArgumentException>(() => AccountNumber.Create(value));
    }
}
