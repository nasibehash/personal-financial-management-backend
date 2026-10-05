using MediatR;
using PersonalFinancialManagement.Application.DTOs;

namespace PersonalFinancialManagement.Application.Features.Commands.DeleteGoalContribution;

// Returns the goal with its updated progress.
public record DeleteGoalContributionCommand(Guid GoalId, Guid ContributionId) : IRequest<GoalDto>;
