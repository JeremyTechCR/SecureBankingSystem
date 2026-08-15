using SecureBanking.Domain.Exceptions;
using SecureBanking.Domain.ValueObjects;

namespace SecureBanking.Domain.Customers;

public sealed class Customer
{
    private const int MaximumNameLength = 100;

    private Customer(Guid id, string firstName, string lastName, EmailAddress email, DateTime createdAtUtc)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Status = CustomerStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public EmailAddress Email { get; private set; }

    public CustomerStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public static Customer Create(
        Guid id,
        string? firstName,
        string? lastName,
        EmailAddress? email,
        DateTime createdAtUtc)
    {
        EnsureIdentifier(id);
        EnsureUtc(createdAtUtc);

        return new Customer(
            id,
            NormalizeName(firstName, "First name"),
            NormalizeName(lastName, "Last name"),
            email ?? throw new InvalidDomainArgumentException("Email address is required."),
            createdAtUtc);
    }

    public void ChangeName(string? firstName, string? lastName)
    {
        EnsureCanBeModified();
        FirstName = NormalizeName(firstName, "First name");
        LastName = NormalizeName(lastName, "Last name");
    }

    public void ChangeEmail(EmailAddress? email)
    {
        EnsureCanBeModified();
        Email = email ?? throw new InvalidDomainArgumentException("Email address is required.");
    }

    public void Suspend()
    {
        if (Status != CustomerStatus.Active)
        {
            throw new InvalidStateTransitionException($"Cannot suspend a customer with status {Status}.");
        }

        Status = CustomerStatus.Suspended;
    }

    public void Reactivate()
    {
        if (Status != CustomerStatus.Suspended)
        {
            throw new InvalidStateTransitionException($"Cannot reactivate a customer with status {Status}.");
        }

        Status = CustomerStatus.Active;
    }

    public void Close()
    {
        if (Status == CustomerStatus.Closed)
        {
            throw new InvalidStateTransitionException("Customer is already closed.");
        }

        Status = CustomerStatus.Closed;
    }

    private static void EnsureIdentifier(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new InvalidDomainArgumentException("Customer identifier cannot be empty.");
        }
    }

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new InvalidDomainArgumentException("Creation date must be expressed in UTC.");
        }
    }

    private static string NormalizeName(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDomainArgumentException($"{fieldName} is required.");
        }

        string normalized = value.Trim();
        if (normalized.Length > MaximumNameLength)
        {
            throw new InvalidDomainArgumentException($"{fieldName} cannot exceed {MaximumNameLength} characters.");
        }

        return normalized;
    }

    private void EnsureCanBeModified()
    {
        if (Status == CustomerStatus.Closed)
        {
            throw new InvalidStateTransitionException("A closed customer cannot be modified.");
        }
    }
}
