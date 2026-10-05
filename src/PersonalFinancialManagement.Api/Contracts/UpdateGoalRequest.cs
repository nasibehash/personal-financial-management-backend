namespace PersonalFinancialManagement.Api.Contracts;

// isCancelled = true cancels the goal, false reopens it.
public record UpdateGoalRequest(
    string Name,
    decimal TargetAmount,
    DateTime? Deadline = null,
    string? Description = null,
    bool IsCancelled = false);
