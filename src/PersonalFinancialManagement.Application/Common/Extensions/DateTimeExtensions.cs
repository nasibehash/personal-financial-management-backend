namespace PersonalFinancialManagement.Application.Common.Extensions;

internal static class DateTimeExtensions
{
    // Dates coming from the API without a time zone are treated as UTC.
    public static DateTime ToUtc(this DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
