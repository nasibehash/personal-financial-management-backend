using MediatR;
using PersonalFinancialManagement.Application.DTOs;

namespace PersonalFinancialManagement.Application.Features.Commands.CreateGoal;

// StartDate defaults to today. Deadline is optional but must be after the start date.
public record CreateGoalCommand(
    string Name,
    decimal TargetAmount,
    DateTime? Deadline = null,
    string? Description = null,
    DateTime? StartDate = null) : IRequest<GoalDto>;
