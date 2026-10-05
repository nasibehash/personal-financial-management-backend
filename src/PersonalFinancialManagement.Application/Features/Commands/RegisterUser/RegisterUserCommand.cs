using MediatR;
using PersonalFinancialManagement.Application.DTOs;

namespace PersonalFinancialManagement.Application.Features.Commands.RegisterUser;

public record RegisterUserCommand(string FullName, string Email, string Password) : IRequest<AuthResponse>;
