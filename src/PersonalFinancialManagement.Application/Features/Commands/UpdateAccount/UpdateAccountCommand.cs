using MediatR;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Features.Commands.UpdateAccount;

// Archiving hides an account from new transactions while keeping its history.
public record UpdateAccountCommand(Guid Id, string Name, AccountType Type, decimal InitialBalance, bool IsArchived)
    : IRequest<AccountDto>;
