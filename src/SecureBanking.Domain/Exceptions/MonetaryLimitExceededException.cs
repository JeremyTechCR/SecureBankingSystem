namespace SecureBanking.Domain.Exceptions;

public sealed class MonetaryLimitExceededException : DomainException
{
    public MonetaryLimitExceededException(string message)
        : base(message)
    {
    }

    public MonetaryLimitExceededException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
