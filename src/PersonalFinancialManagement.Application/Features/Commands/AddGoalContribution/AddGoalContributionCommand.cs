using MediatR;
using PersonalFinancialManagement.Application.DTOs;

namespace PersonalFinancialManagement.Application.Features.Commands.AddGoalContribution;

// Returns the goal with its updated progress. Date defaults to now.
public record AddGoalContributionCommand(Guid GoalId, decimal Amount, DateTime? Date = null, string? Note = null)
    : IRequest<GoalDto>;
