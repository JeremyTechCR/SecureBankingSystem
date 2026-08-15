namespace SecureBanking.Domain.Exceptions;

public sealed class InsufficientFundsException : DomainException
{
    public InsufficientFundsException(string message)
        : base(message)
    {
    }
}
