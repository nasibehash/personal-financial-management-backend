namespace PersonalFinancialManagement.Application.DTOs;

public record PeriodTotalsDto(DateTime From, DateTime To, decimal TotalIncome, decimal TotalExpense, decimal NetAmount);

// Compares a period with the period of the same length right before it.
// A change percentage is null when the previous value was zero.
public record PeriodComparisonDto(
    PeriodTotalsDto Current,
    PeriodTotalsDto Previous,
    decimal? IncomeChangePercent,
    decimal? ExpenseChangePercent);
