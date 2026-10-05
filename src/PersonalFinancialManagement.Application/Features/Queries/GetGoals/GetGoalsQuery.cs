using MediatR;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Features.Queries.GetGoals;

// Status = null returns all goals. Active goals come first, those with the nearest deadline on top.
public record GetGoalsQuery(GoalStatus? Status = null) : IRequest<IReadOnlyList<GoalDto>>;
