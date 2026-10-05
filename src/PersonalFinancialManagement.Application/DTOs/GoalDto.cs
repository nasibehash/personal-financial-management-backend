using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.DTOs;

// CurrentAmount is the sum of the contributions. DaysLeft is negative once the deadline has passed.
// RequiredMonthlySaving is what must be saved per month to reach the target by the deadline
// (null when there is no deadline, nothing is left to save, or the goal is not active).
public record GoalDto(
    Guid Id,
    string Name,
    string? Description,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal RemainingAmount,
    decimal ProgressPercent,
    DateTime StartDate,
    DateTime? Deadline,
    int? DaysLeft,
    decimal? RequiredMonthlySaving,
    bool IsOverdue,
    GoalStatus Status);
