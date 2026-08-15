using SecureBanking.Domain.Accounts;
using SecureBanking.Domain.Exceptions;
using SecureBanking.Domain.ValueObjects;

namespace SecureBanking.Domain.UnitTests.Accounts;

public sealed class BankAccountTests
{
    private static readonly DateTime CreatedAtUtc = new(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidData_CreatesActiveAccountWithZeroBalance()
    {
        Guid id = Guid.NewGuid();
        Guid customerId = Guid.NewGuid();
        BankAccount account = BankAccount.Create(id, customerId, AccountNumber.Create("001234567890"), AccountType.Savings, Currency.Crc, CreatedAtUtc);

        Assert.Equal(id, account.Id);
        Assert.Equal(customerId, account.CustomerId);
        Assert.Equal(AccountType.Savings, account.Type);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Equal(Money.Zero(Currency.Crc), account.Balance);
        Assert.Equal(CreatedAtUtc, account.CreatedAtUtc);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Create_WithEmptyIdentifier_ThrowsDomainException(bool emptyAccountId, bool emptyCustomerId)
    {
        Assert.Throws<InvalidDomainArgumentException>(() => BankAccount.Create(
            emptyAccountId ? Guid.Empty : Guid.NewGuid(),
            emptyCustomerId ? Guid.Empty : Guid.NewGuid(),
            AccountNumber.Create("001234567890"), AccountType.Checking, Currency.Usd, CreatedAtUtc));
    }

    [Fact]
    public void Create_WithNonUtcDate_ThrowsDomainException()
    {
        Assert.Throws<InvalidDomainArgumentException>(() => BankAccount.Create(
            Guid.NewGuid(), Guid.NewGuid(), AccountNumber.Create("001234567890"), AccountType.Checking,
            Currency.Usd, DateTime.SpecifyKind(CreatedAtUtc, DateTimeKind.Unspecified)));
    }

    [Fact]
    public void Credit_WithPositiveMatchingAmount_IncreasesBalance()
    {
        BankAccount account = CreateAccount();
        account.Credit(new Money(25.50m, Currency.Usd));
        Assert.Equal(new Money(25.50m, Currency.Usd), account.Balance);
    }

    [Fact]
    public void Debit_WithAvailableFunds_DecreasesBalance()
    {
        BankAccount account = CreateAccount();
        account.Credit(new Money(20m, Currency.Usd));
        account.Debit(new Money(7.25m, Currency.Usd));
        Assert.Equal(new Money(12.75m, Currency.Usd), account.Balance);
    }

    [Fact]
    public void Debit_WithoutEnoughFunds_ThrowsAndPreservesBalance()
    {
        BankAccount account = CreateAccount();
        account.Credit(new Money(5m, Currency.Usd));
        Assert.Throws<InsufficientFundsException>(() => account.Debit(new Money(6m, Currency.Usd)));
        Assert.Equal(new Money(5m, Currency.Usd), account.Balance);
    }

    [Fact]
    public void MonetaryOperation_WithDifferentCurrency_ThrowsCurrencyMismatch()
    {
        BankAccount account = CreateAccount();
        Assert.Throws<CurrencyMismatchException>(() => account.Credit(new Money(1m, Currency.Crc)));
        Assert.Throws<CurrencyMismatchException>(() => account.Debit(new Money(1m, Currency.Crc)));
    }

    [Fact]
    public void MonetaryOperation_WithZeroAmount_ThrowsDomainException()
    {
        BankAccount account = CreateAccount();
        Assert.Throws<InvalidDomainArgumentException>(() => account.Credit(Money.Zero(Currency.Usd)));
        Assert.Throws<InvalidDomainArgumentException>(() => account.Debit(Money.Zero(Currency.Usd)));
    }

    [Fact]
    public void FreezeThenUnfreeze_PerformsValidTransitions()
    {
        BankAccount account = CreateAccount();
        account.Freeze();
        Assert.Equal(AccountStatus.Frozen, account.Status);
        account.Unfreeze();
        Assert.Equal(AccountStatus.Active, account.Status);
    }

    [Fact]
    public void FrozenAccount_RejectsMonetaryOperations()
    {
        BankAccount account = CreateAccount();
        account.Freeze();
        Assert.Throws<InvalidAccountStateException>(() => account.Credit(new Money(1m, Currency.Usd)));
        Assert.Throws<InvalidAccountStateException>(() => account.Debit(new Money(1m, Currency.Usd)));
    }

    [Fact]
    public void Close_WithNonZeroBalance_ThrowsAndPreservesStatus()
    {
        BankAccount account = CreateAccount();
        account.Credit(new Money(1m, Currency.Usd));
        Assert.Throws<InvalidAccountStateException>(() => account.Close());
        Assert.Equal(AccountStatus.Active, account.Status);
    }

    [Fact]
    public void Close_WithZeroBalance_ClosesAccountIrreversibly()
    {
        BankAccount account = CreateAccount();
        account.Close();
        Assert.Equal(AccountStatus.Closed, account.Status);
        Assert.Throws<InvalidStateTransitionException>(() => account.Close());
        Assert.Throws<InvalidStateTransitionException>(() => account.Unfreeze());
        Assert.Throws<InvalidAccountStateException>(() => account.Credit(new Money(1m, Currency.Usd)));
    }

    [Fact]
    public void RedundantStateTransition_ThrowsDomainException()
    {
        BankAccount account = CreateAccount();
        Assert.Throws<InvalidStateTransitionException>(() => account.Unfreeze());
        account.Freeze();
        Assert.Throws<InvalidStateTransitionException>(() => account.Freeze());
    }

    private static BankAccount CreateAccount() => BankAccount.Create(
        Guid.NewGuid(), Guid.NewGuid(), AccountNumber.Create("001234567890"),
        AccountType.Checking, Currency.Usd, CreatedAtUtc);
}
