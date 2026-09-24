using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SecureBanking.Domain.ValueObjects;

namespace SecureBanking.Infrastructure.Persistence.Converters;

internal sealed class AccountNumberConverter : ValueConverter<AccountNumber, string>
{
    public AccountNumberConverter()
        : base(number => number.GetFullValue(), value => AccountNumber.Create(value))
    {
    }
}
