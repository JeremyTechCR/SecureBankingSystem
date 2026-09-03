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
        EmailAddress first = EmailAddress.Create("User@Example.com");
        EmailAddress second = EmailAddress.Create("user@example.com");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void DifferentValues_AreNotEqual()
    {
        EmailAddress email = EmailAddress.Create("user@example.com");

        Assert.NotEqual(email, EmailAddress.Create("other@example.com"));
        Assert.False(email.Equals("user@example.com"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("@example.com")]
    [InlineData("user@localhost")]
    [InlineData("user @example.com")]
    [InlineData("user@@example.com")]
    [InlineData(".user@example.com")]
    [InlineData("user.@example.com")]
    [InlineData("user..name@example.com")]
    [InlineData("user@.example.com")]
    [InlineData("user@example..com")]
    [InlineData("user@example.com.")]
    [InlineData("user@-example.com")]
    [InlineData("user@example-.com")]
    public void Create_WithInvalidValue_ThrowsDomainException(string? value)
    {
        Assert.Throws<InvalidDomainArgumentException>(() => EmailAddress.Create(value));
    }

    [Fact]
    public void Create_WithLocalPartAtMaximumLength_Succeeds()
    {
        string value = $"{new string('a', 64)}@example.com";

        Assert.Equal(value, EmailAddress.Create(value).Value);
    }

    [Fact]
    public void Create_WithLocalPartOverMaximumLength_ThrowsDomainException()
    {
        string value = $"{new string('a', 65)}@example.com";

        Assert.Throws<InvalidDomainArgumentException>(() => EmailAddress.Create(value));
    }

    [Fact]
    public void Create_WithAddressAtMaximumLength_Succeeds()
    {
        string domain = $"{new string('a', 63)}.{new string('b', 63)}.{new string('c', 61)}";
        string value = $"{new string('d', 64)}@{domain}";

        Assert.Equal(254, value.Length);
        Assert.Equal(value, EmailAddress.Create(value).Value);
    }

    [Fact]
    public void Create_WithAddressOverMaximumLength_ThrowsDomainException()
    {
        string domain = $"{new string('a', 63)}.{new string('b', 63)}.{new string('c', 62)}";
        string value = $"{new string('d', 64)}@{domain}";

        Assert.Equal(255, value.Length);
        Assert.Throws<InvalidDomainArgumentException>(() => EmailAddress.Create(value));
    }
}
