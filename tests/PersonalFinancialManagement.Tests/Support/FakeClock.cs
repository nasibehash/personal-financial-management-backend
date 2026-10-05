using PersonalFinancialManagement.Application.Interfaces;

namespace PersonalFinancialManagement.Tests.Support;

public class FakeClock : IDateTimeProvider
{
    public FakeClock(DateTime utcNow) => UtcNow = utcNow;

    public DateTime UtcNow { get; set; }
}
