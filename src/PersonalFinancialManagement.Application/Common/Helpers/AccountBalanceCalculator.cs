using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Common.Helpers;

internal static class AccountBalanceCalculator
{
    // Balance = initial balance + income - expenses - transfers out + transfers in.
    // Returns the current balance of every account owned by the user, keyed by account id.
    public static async Task<Dictionary<Guid, decimal>> CalculateAsync(
        IApplicationDbContext db, Guid userId, CancellationToken cancellationToken)
    {
        var accounts = await db.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new { a.Id, a.InitialBalance })
            .ToListAsync(cancellationToken);

        var balances = accounts.ToDictionary(a => a.Id, a => a.InitialBalance);

        var movements = await db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .GroupBy(t => new { t.AccountId, t.Type })
            .Select(g => new { g.Key.AccountId, g.Key.Type, Total = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);

        foreach (var movement in movements)
        {
            if (!balances.ContainsKey(movement.AccountId))
                continue;

            // Income adds; expenses and outgoing transfers subtract.
            balances[movement.AccountId] += movement.Type == TransactionType.Income ? movement.Total : -movement.Total;
        }

        var incomingTransfers = await db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Type == TransactionType.Transfer && t.DestinationAccountId != null)
            .GroupBy(t => t.DestinationAccountId)
            .Select(g => new { AccountId = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);

        foreach (var transfer in incomingTransfers)
        {
            if (transfer.AccountId is { } accountId && balances.ContainsKey(accountId))
                balances[accountId] += transfer.Total;
        }

        return balances;
    }
}
