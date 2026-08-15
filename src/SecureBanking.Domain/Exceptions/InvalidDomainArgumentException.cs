namespace SecureBanking.Domain.Exceptions;

public sealed class InvalidDomainArgumentException : DomainException
{
    public InvalidDomainArgumentException(string message)
        : base(message)
    {
    }
}
