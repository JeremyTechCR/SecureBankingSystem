using SecureBanking.Domain.Exceptions;

namespace SecureBanking.Domain.ValueObjects;

public sealed record EmailAddress
{
    private EmailAddress(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static EmailAddress Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDomainArgumentException("Email address is required.");
        }

        string normalized = value.Trim().ToLowerInvariant();
        int separatorIndex = normalized.IndexOf('@');

        if (normalized.Length > 254 || normalized.Any(char.IsWhiteSpace) ||
            separatorIndex <= 0 || separatorIndex != normalized.LastIndexOf('@') ||
            separatorIndex == normalized.Length - 1)
        {
            throw new InvalidDomainArgumentException("Email address format is invalid.");
        }

        string localPart = normalized[..separatorIndex];
        string domain = normalized[(separatorIndex + 1)..];

        if (localPart.Length > 64 || HasInvalidDots(localPart) ||
            HasInvalidDots(domain) || !domain.Contains('.') || HasInvalidDomainLabel(domain))
        {
            throw new InvalidDomainArgumentException("Email address format is invalid.");
        }

        return new EmailAddress(normalized);
    }

    public override string ToString() => Value;

    private static bool HasInvalidDots(string value) =>
        value.StartsWith('.') || value.EndsWith('.') || value.Contains("..", StringComparison.Ordinal);

    private static bool HasInvalidDomainLabel(string domain) =>
        domain.Split('.').Any(label =>
            label.Length == 0 || label.StartsWith('-') || label.EndsWith('-'));
}
