using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Domain.Entities;

namespace PersonalFinancialManagement.Application.Common.Helpers;

internal static class AccountMappings
{
    public static AccountDto ToDto(this Account account, decimal balance)
        => new(account.Id, account.Name, account.Type, account.InitialBalance, balance, account.IsArchived);
}
