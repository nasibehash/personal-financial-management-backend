using MediatR;
using PersonalFinancialManagement.Application.DTOs;

namespace PersonalFinancialManagement.Application.Features.Commands.UpdateGoal;

// IsCancelled = true cancels the goal; false reopens it (it becomes Active or Completed depending on its progress).
public record UpdateGoalCommand(
    Guid Id,
    string Name,
    decimal TargetAmount,
    DateTime? Deadline = null,
    string? Description = null,
    bool IsCancelled = false) : IRequest<GoalDto>;
