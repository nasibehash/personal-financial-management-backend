using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Api.Contracts;

public record UpdateAccountRequest(string Name, AccountType Type, decimal InitialBalance, bool IsArchived);
