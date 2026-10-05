using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Domain.Entities;

namespace PersonalFinancialManagement.Application.Common.Helpers;

internal static class TransactionMappings
{
    public static IQueryable<TransactionDto> ProjectToDto(this IQueryable<Transaction> query)
        => query.Select(t => new TransactionDto(
            t.Id,
            t.Type,
            t.Amount,
            t.Date,
            t.Description,
            t.AccountId,
            t.Account!.Name,
            t.DestinationAccountId,
            t.DestinationAccount != null ? t.DestinationAccount.Name : null,
            t.CategoryId,
            t.Category != null ? t.Category.Name : null,
            t.Source));
}
