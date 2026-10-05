using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.DTOs;

// Balance is the current balance (initial balance plus all transactions).
public record AccountDto(Guid Id, string Name, AccountType Type, decimal InitialBalance, decimal Balance, bool IsArchived);
