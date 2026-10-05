namespace PersonalFinancialManagement.Application.DTOs;

// From and To are inclusive dates. Transfers between the user's own accounts are not counted
// as income or expense. SavingsRate is a percentage of income (null when there is no income).
// TotalBalance is the current balance of all non-archived accounts, independent of the period.
public record FinancialSummaryDto(
    DateTime From,
    DateTime To,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetAmount,
    decimal? SavingsRate,
    int TransactionCount,
    decimal AverageDailyExpense,
    decimal TotalBalance);
