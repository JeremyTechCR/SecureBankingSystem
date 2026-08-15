using SecureBanking.Domain.Exceptions;
using SecureBanking.Domain.ValueObjects;

namespace SecureBanking.Domain.UnitTests.ValueObjects;

public sealed class EmailAddressTests
{
    [Fact]
    public void Create_WithValidValue_NormalizesAndCreatesEmail()
    {
        EmailAddress email = EmailAddress.Create("  USER@Example.COM  ");

        Assert.Equal("user@example.com", email.Value);
        Assert.Equal("user@example.com", email.ToString());
    }

    [Fact]
    public void EqualValues_AreEqual()
    {
        Assert.Equal(EmailAddress.Create("user@example.com"), EmailAddress.Create(" USER@EXAMPLE.COM "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("@example.com")]
    [InlineData("user@localhost")]
    [InlineData("user @example.com")]
    [InlineData("user@@example.com")]
    public void Create_WithInvalidValue_ThrowsDomainException(string? value)
    {
        Assert.Throws<InvalidDomainArgumentException>(() => EmailAddress.Create(value));
    }
}
