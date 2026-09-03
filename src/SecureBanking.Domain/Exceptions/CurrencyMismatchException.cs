namespace SecureBanking.Domain.Exceptions;

public sealed class CurrencyMismatchException : DomainException
{
    public CurrencyMismatchException(string message)
        : base(message)
    {
    }
}
