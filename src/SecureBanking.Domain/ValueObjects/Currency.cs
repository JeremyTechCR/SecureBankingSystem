using SecureBanking.Domain.Exceptions;

namespace SecureBanking.Domain.ValueObjects;

public sealed record Currency
{
    private static readonly IReadOnlyDictionary<string, Currency> SupportedCurrencies =
        new Dictionary<string, Currency>(StringComparer.Ordinal)
        {
            ["CRC"] = new("CRC"),
            ["USD"] = new("USD")
        };

    private Currency(string code)
    {
        Code = code;
    }

    public string Code { get; }

    public static Currency Crc => SupportedCurrencies["CRC"];

    public static Currency Usd => SupportedCurrencies["USD"];

    public static Currency From(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidDomainArgumentException("Currency code is required.");
        }

        string normalized = code.Trim().ToUpperInvariant();
        if (!SupportedCurrencies.TryGetValue(normalized, out Currency? currency))
        {
            throw new InvalidDomainArgumentException($"Currency '{normalized}' is not supported.");
        }

        return currency;
    }

    public override string ToString() => Code;
}
