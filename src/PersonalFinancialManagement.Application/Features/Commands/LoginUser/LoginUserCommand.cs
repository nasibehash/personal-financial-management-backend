using MediatR;
using PersonalFinancialManagement.Application.DTOs;

namespace PersonalFinancialManagement.Application.Features.Commands.LoginUser;

public record LoginUserCommand(string Email, string Password) : IRequest<AuthResponse>;
