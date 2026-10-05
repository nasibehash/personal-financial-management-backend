using MediatR;

namespace PersonalFinancialManagement.Application.Features.Commands.DeleteGoal;

public record DeleteGoalCommand(Guid Id) : IRequest;
