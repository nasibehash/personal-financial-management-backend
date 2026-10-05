namespace PersonalFinancialManagement.Application.Common.Models;

public record CategoryShare(string Name, decimal Total, decimal Percentage);

public record GoalSnapshot(
    string Name,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal ProgressPercent,
    int? DaysLeft,
    decimal? RequiredMonthlySaving);

// The numbers insights are based on: one period, the period before it, and the user's active goals.
public record FinancialSnapshot(
    DateTime From,
    DateTime To,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal PreviousIncome,
    decimal PreviousExpense,
    decimal TotalBalance,
    IReadOnlyList<CategoryShare> TopExpenseCategories,
    IReadOnlyList<GoalSnapshot> Goals);

// Items are short, user-facing sentences. GeneratedByAi is false when the built-in rules produced them.
public record FinancialInsights(IReadOnlyList<string> Items, bool GeneratedByAi);
