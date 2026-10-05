using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Domain.Entities;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Common.Helpers;

internal static class GoalProgress
{
    private const decimal AverageDaysPerMonth = 30.4375m;

    public static GoalDto ToDto(Goal goal, decimal currentAmount, DateTime utcNow)
    {
        var remaining = Math.Max(goal.TargetAmount - currentAmount, 0);
        var progress = goal.TargetAmount <= 0
            ? 0
            : Math.Min(Math.Round(currentAmount / goal.TargetAmount * 100, 1, MidpointRounding.AwayFromZero), 100);

        int? daysLeft = goal.Deadline is { } deadline ? (int)(deadline.Date - utcNow.Date).TotalDays : null;
        var isActive = goal.Status == GoalStatus.Active;
        var isOverdue = isActive && daysLeft is < 0;

        decimal? requiredMonthly = null;
        if (isActive && remaining > 0 && daysLeft is > 0)
        {
            var months = Math.Max(daysLeft.Value / AverageDaysPerMonth, 1m);
            requiredMonthly = Math.Round(remaining / months, 2, MidpointRounding.AwayFromZero);
        }

        return new GoalDto(
            goal.Id,
            goal.Name,
            goal.Description,
            goal.TargetAmount,
            currentAmount,
            remaining,
            progress,
            goal.StartDate,
            goal.Deadline,
            daysLeft,
            requiredMonthly,
            isOverdue,
            goal.Status);
    }

    // A goal is completed once the contributions reach the target and active again if they drop below it.
    // Cancelled goals stay cancelled until the user reopens them.
    public static void Reevaluate(Goal goal, decimal currentAmount, DateTime utcNow)
    {
        if (goal.Status == GoalStatus.Cancelled)
            return;

        if (currentAmount >= goal.TargetAmount)
        {
            goal.Status = GoalStatus.Completed;
            goal.CompletedAtUtc ??= utcNow;
        }
        else
        {
            goal.Status = GoalStatus.Active;
            goal.CompletedAtUtc = null;
        }
    }
}
