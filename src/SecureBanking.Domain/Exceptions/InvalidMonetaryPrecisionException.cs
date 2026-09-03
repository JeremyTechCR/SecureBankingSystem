namespace SecureBanking.Domain.Exceptions;

public sealed class InvalidMonetaryPrecisionException : DomainException
{
    public InvalidMonetaryPrecisionException(string message)
        : base(message)
    {
    }
}
