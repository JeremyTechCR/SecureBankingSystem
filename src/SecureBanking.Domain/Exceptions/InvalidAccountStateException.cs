namespace SecureBanking.Domain.Exceptions;

public sealed class InvalidAccountStateException : DomainException
{
    public InvalidAccountStateException(string message)
        : base(message)
    {
    }
}
