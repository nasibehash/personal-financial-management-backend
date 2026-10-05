using PersonalFinancialManagement.Application.Interfaces;

namespace PersonalFinancialManagement.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
