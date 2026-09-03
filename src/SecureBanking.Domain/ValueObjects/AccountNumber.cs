using SecureBanking.Domain.Exceptions;

namespace SecureBanking.Domain.ValueObjects;

public sealed class AccountNumber : IEquatable<AccountNumber>
{
    private const int RequiredLength = 12;

    private AccountNumber(string value)
    {
        Value = value;
    }

    private string Value { get; }

    public static AccountNumber Create(string? value)
    {
        if (value is null || value.Length != RequiredLength || value.Any(character => character is < '0' or > '9'))
        {
            throw new InvalidDomainArgumentException("Account number must contain exactly 12 numeric digits.");
        }

        return new AccountNumber(value);
    }

    public string GetFullValue() => Value;

    public string ToMaskedString() => $"********{Value[^4..]}";

    public bool Equals(AccountNumber? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is AccountNumber other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => ToMaskedString();
}
