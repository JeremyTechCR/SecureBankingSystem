using SecureBanking.Domain.Customers;
using SecureBanking.Domain.Exceptions;
using SecureBanking.Domain.ValueObjects;

namespace SecureBanking.Domain.UnitTests.Customers;

public sealed class CustomerTests
{
    private static readonly DateTime CreatedAtUtc = new(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidData_CreatesActiveCustomerAndNormalizesNames()
    {
        Guid id = Guid.NewGuid();

        Customer customer = Customer.Create(id, "  Ada ", " Lovelace  ", EmailAddress.Create("ADA@example.com"), CreatedAtUtc);

        Assert.Equal(id, customer.Id);
        Assert.Equal("Ada", customer.FirstName);
        Assert.Equal("Lovelace", customer.LastName);
        Assert.Equal("ada@example.com", customer.Email.Value);
        Assert.Equal(CustomerStatus.Active, customer.Status);
        Assert.Equal(CreatedAtUtc, customer.CreatedAtUtc);
    }

    [Theory]
    [InlineData(true, "Ada", "Lovelace")]
    [InlineData(false, "", "Lovelace")]
    [InlineData(false, "Ada", " ")]
    public void Create_WithInvalidData_ThrowsDomainException(bool emptyId, string firstName, string lastName)
    {
        Guid id = emptyId ? Guid.Empty : Guid.NewGuid();

        Assert.Throws<InvalidDomainArgumentException>(
            () => Customer.Create(id, firstName, lastName, EmailAddress.Create("ada@example.com"), CreatedAtUtc));
    }

    [Fact]
    public void Create_WithNonUtcDate_ThrowsDomainException()
    {
        DateTime localDate = new(2026, 8, 14, 12, 0, 0, DateTimeKind.Local);

        Assert.Throws<InvalidDomainArgumentException>(
            () => Customer.Create(Guid.NewGuid(), "Ada", "Lovelace", EmailAddress.Create("ada@example.com"), localDate));
    }

    [Fact]
    public void ChangeNameAndEmail_WithOpenCustomer_UpdatesData()
    {
        Customer customer = CreateCustomer();

        customer.ChangeName(" Grace ", " Hopper ");
        customer.ChangeEmail(EmailAddress.Create("GRACE@example.com"));

        Assert.Equal("Grace", customer.FirstName);
        Assert.Equal("Hopper", customer.LastName);
        Assert.Equal("grace@example.com", customer.Email.Value);
    }

    [Fact]
    public void SuspendThenReactivate_PerformsValidTransitions()
    {
        Customer customer = CreateCustomer();

        customer.Suspend();
        Assert.Equal(CustomerStatus.Suspended, customer.Status);

        customer.Reactivate();
        Assert.Equal(CustomerStatus.Active, customer.Status);
    }

    [Fact]
    public void RedundantStateTransition_ThrowsDomainException()
    {
        Customer customer = CreateCustomer();

        Assert.Throws<InvalidStateTransitionException>(() => customer.Reactivate());
        customer.Suspend();
        Assert.Throws<InvalidStateTransitionException>(() => customer.Suspend());
    }

    [Fact]
    public void ClosedCustomer_CannotBeModifiedOrReactivated()
    {
        Customer customer = CreateCustomer();
        customer.Close();

        Assert.Equal(CustomerStatus.Closed, customer.Status);
        Assert.Throws<InvalidStateTransitionException>(() => customer.ChangeName("Grace", "Hopper"));
        Assert.Throws<InvalidStateTransitionException>(() => customer.ChangeEmail(EmailAddress.Create("grace@example.com")));
        Assert.Throws<InvalidStateTransitionException>(() => customer.Reactivate());
        Assert.Throws<InvalidStateTransitionException>(() => customer.Close());
    }

    private static Customer CreateCustomer() => Customer.Create(
        Guid.NewGuid(),
        "Ada",
        "Lovelace",
        EmailAddress.Create("ada@example.com"),
        CreatedAtUtc);
}
