using SecureBanking.Domain.Exceptions;
using SecureBanking.Domain.ValueObjects;

namespace SecureBanking.Domain.Accounts;

public sealed class BankAccount
{
    private BankAccount(
        Guid id,
        Guid customerId,
        AccountNumber accountNumber,
        AccountType type,
        Currency currency,
        DateTime createdAtUtc)
    {
        Id = id;
        CustomerId = customerId;
        AccountNumber = accountNumber;
        Type = type;
        Status = AccountStatus.Active;
        Currency = currency;
        Balance = Money.Zero(currency);
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid CustomerId { get; }

    public AccountNumber AccountNumber { get; }

    public AccountType Type { get; }

    public AccountStatus Status { get; private set; }

    public Currency Currency { get; }

    public Money Balance { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public static BankAccount Create(
        Guid id,
        Guid customerId,
        AccountNumber? accountNumber,
        AccountType type,
        Currency? currency,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty || customerId == Guid.Empty)
        {
            throw new InvalidDomainArgumentException("Account and customer identifiers cannot be empty.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new InvalidDomainArgumentException("Account type is invalid.");
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new InvalidDomainArgumentException("Creation date must be expressed in UTC.");
        }

        return new BankAccount(
            id,
            customerId,
            accountNumber ?? throw new InvalidDomainArgumentException("Account number is required."),
            type,
            currency ?? throw new InvalidDomainArgumentException("Currency is required."),
            createdAtUtc);
    }

    public void Credit(Money? amount)
    {
        EnsureMonetaryOperationAllowed();
        EnsurePositiveAmount(amount);
        Balance = Balance.Add(amount!);
    }

    public void Debit(Money? amount)
    {
        EnsureMonetaryOperationAllowed();
        EnsurePositiveAmount(amount);
        Balance = Balance.Subtract(amount!);
    }

    public void Freeze()
    {
        if (Status != AccountStatus.Active)
        {
            throw new InvalidStateTransitionException($"Cannot freeze an account with status {Status}.");
        }

        Status = AccountStatus.Frozen;
    }

    public void Unfreeze()
    {
        if (Status != AccountStatus.Frozen)
        {
            throw new InvalidStateTransitionException($"Cannot unfreeze an account with status {Status}.");
        }

        Status = AccountStatus.Active;
    }

    public void Close()
    {
        if (Status == AccountStatus.Closed)
        {
            throw new InvalidStateTransitionException("Account is already closed.");
        }

        if (Balance.Amount != 0m)
        {
            throw new InvalidAccountStateException("Only an account with a zero balance can be closed.");
        }

        Status = AccountStatus.Closed;
    }

    private void EnsureMonetaryOperationAllowed()
    {
        if (Status != AccountStatus.Active)
        {
            throw new InvalidAccountStateException($"Monetary operations require an active account; current status is {Status}.");
        }
    }

    private void EnsurePositiveAmount(Money? amount)
    {
        if (amount is null || amount.Amount <= 0m)
        {
            throw new InvalidDomainArgumentException("Transaction amount must be greater than zero.");
        }

        if (amount.Currency != Currency)
        {
            throw new CurrencyMismatchException("Transaction currency must match the account currency.");
        }
    }
}
