using MediatR;
using PersonalFinancialManagement.Application.DTOs;

namespace PersonalFinancialManagement.Application.Features.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<UserDto>;
