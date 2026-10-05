using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Domain.Entities;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Common.Helpers;

// Checks that the accounts and category a transaction points at belong to the user and fit the transaction.
internal static class TransactionReferenceChecker
{
    // existing: the transaction being edited, if any. Accounts it already uses may stay archived.
    public static async Task EnsureValidAsync(
        IApplicationDbContext db,
        Guid userId,
        TransactionType type,
        Guid accountId,
        Guid? destinationAccountId,
        Guid? categoryId,
        Transaction? existing,
        CancellationToken cancellationToken)
    {
        var accountIds = new List<Guid> { accountId };
        if (type == TransactionType.Transfer && destinationAccountId is { } destinationId)
            accountIds.Add(destinationId);

        var accounts = await db.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId && accountIds.Contains(a.Id))
            .Select(a => new { a.Id, a.Name, a.IsArchived })
            .ToListAsync(cancellationToken);

        foreach (var id in accountIds.Distinct())
        {
            var account = accounts.FirstOrDefault(a => a.Id == id)
                          ?? throw new NotFoundException("Account", id);

            var alreadyUsed = existing is not null && (existing.AccountId == id || existing.DestinationAccountId == id);
            if (account.IsArchived && !alreadyUsed)
                throw new BusinessRuleException($"Account '{account.Name}' is archived.");
        }

        if (type == TransactionType.Transfer)
            return;

        var category = await db.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.Id == categoryId)
            .Select(c => new { c.Type })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Category", categoryId?.ToString() ?? "(none)");

        var expected = type == TransactionType.Income ? CategoryType.Income : CategoryType.Expense;
        if (category.Type != expected)
            throw new BusinessRuleException($"{type} transactions need an {expected.ToString().ToLowerInvariant()} category.");
    }
}
