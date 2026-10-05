using MediatR;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Features.Commands.CreateAccount;

public record CreateAccountCommand(string Name, AccountType Type, decimal InitialBalance = 0) : IRequest<AccountDto>;
