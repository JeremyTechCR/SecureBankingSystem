using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SecureBanking.Infrastructure.Persistence.Converters;

internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(value => EnsureUtc(value), value => SpecifyUtc(value))
    {
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new InvalidOperationException("Only UTC DateTime values can be persisted.");
        }

        return value;
    }

    private static DateTime SpecifyUtc(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
