namespace PersonalFinancialManagement.Application.Interfaces;

// Abstraction over the clock so time-dependent logic can be tested.
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
