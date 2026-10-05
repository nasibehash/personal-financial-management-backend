namespace PersonalFinancialManagement.Application.DTOs;

public record MonthlyTrendItemDto(int Year, int Month, decimal Income, decimal Expense, decimal Net);
