namespace PersonalFinancialManagement.Application.Common.Models;

// A range of whole days: From is the first day, ToExclusive the day after the last one.
internal readonly record struct DateRange(DateTime From, DateTime ToExclusive)
{
    public DateTime ToInclusive => ToExclusive.AddDays(-1);

    public int Days => Math.Max((int)(ToExclusive - From).TotalDays, 1);

    // Defaults to the current month up to today.
    public static DateRange ForReport(DateTime? from, DateTime? to, DateTime utcNow)
    {
        var today = utcNow.Date;
        var start = (from ?? new DateTime(today.Year, today.Month, 1)).Date;
        var end = (to ?? today).Date;
        return new DateRange(start, end.AddDays(1));
    }

    // The range of the same length that ends right before this one starts.
    public DateRange Previous() => new(From.AddDays(-Days), From);
}
