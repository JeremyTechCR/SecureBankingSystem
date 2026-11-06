using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SecureBanking.Domain.ValueObjects;

namespace SecureBanking.Infrastructure.Persistence.Converters;

internal sealed class EmailAddressConverter : ValueConverter<EmailAddress, string>
{
    public EmailAddressConverter()
        : base(email => email.Value, value => EmailAddress.Create(value))
    {
    }
}
