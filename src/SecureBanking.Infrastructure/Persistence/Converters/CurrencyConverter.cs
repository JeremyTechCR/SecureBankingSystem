using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SecureBanking.Domain.ValueObjects;

namespace SecureBanking.Infrastructure.Persistence.Converters;

internal sealed class CurrencyConverter : ValueConverter<Currency, string>
{
    public CurrencyConverter()
        : base(currency => currency.Code, value => Currency.From(value))
    {
    }
}
