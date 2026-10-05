using MediatR;
using PersonalFinancialManagement.Application.DTOs;

namespace PersonalFinancialManagement.Application.Features.Queries.GetGoalById;

public record GetGoalByIdQuery(Guid Id) : IRequest<GoalDetailDto>;
