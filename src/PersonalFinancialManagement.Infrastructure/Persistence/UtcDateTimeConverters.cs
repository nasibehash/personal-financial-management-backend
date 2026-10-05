using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PersonalFinancialManagement.Infrastructure.Persistence;

// PostgreSQL's "timestamp with time zone" only accepts UTC values, and dates coming from the API
// or from model binding often have Kind = Unspecified. These converters make every date UTC on the
// way in (including values used in query filters) and mark it as UTC on the way out.
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(value => ToUtc(value), value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }

    internal static DateTime ToUtc(DateTime value) => value.Kind == DateTimeKind.Local
        ? value.ToUniversalTime()
        : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}

public class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter() : base(
        value => value.HasValue ? UtcDateTimeConverter.ToUtc(value.Value) : value,
        value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value)
    {
    }
}
