using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Models;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Common.Helpers;

internal record PeriodTotals(decimal Income, decimal Expense, int TransactionCount)
{
    public decimal Net => Income - Expense;
}

internal record CategoryTotal(Guid CategoryId, string Name, decimal Total, int Count);

// Queries shared by the report handlers and the AI insights. Transfers are never counted.
internal static class ReportQueries
{
    public static async Task<PeriodTotals> GetTotalsAsync(
        IApplicationDbContext db, Guid userId, DateRange range, CancellationToken cancellationToken)
    {
        var from = range.From;
        var to = range.ToExclusive;

        var groups = await db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Date >= from && t.Date < to && t.Type != TransactionType.Transfer)
            .GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Total = g.Sum(t => t.Amount), Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new PeriodTotals(
            groups.Where(g => g.Type == TransactionType.Income).Sum(g => g.Total),
            groups.Where(g => g.Type == TransactionType.Expense).Sum(g => g.Total),
            groups.Sum(g => g.Count));
    }

    public static async Task<List<CategoryTotal>> GetCategoryTotalsAsync(
        IApplicationDbContext db, Guid userId, DateRange range, TransactionType type, CancellationToken cancellationToken)
    {
        var from = range.From;
        var to = range.ToExclusive;

        var rows = await db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Date >= from && t.Date < to && t.Type == type && t.CategoryId != null)
            .GroupBy(t => new { t.CategoryId, t.Category!.Name })
            .Select(g => new { g.Key.CategoryId, g.Key.Name, Total = g.Sum(t => t.Amount), Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new CategoryTotal(r.CategoryId!.Value, r.Name, r.Total, r.Count))
            .OrderByDescending(r => r.Total)
            .ThenBy(r => r.Name)
            .ToList();
    }

    // Sum of the current balances of all non-archived accounts.
    public static async Task<decimal> GetTotalBalanceAsync(
        IApplicationDbContext db, Guid userId, CancellationToken cancellationToken)
    {
        var activeAccountIds = await db.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId && !a.IsArchived)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var balances = await AccountBalanceCalculator.CalculateAsync(db, userId, cancellationToken);
        return activeAccountIds.Sum(id => balances[id]);
    }

    public static decimal? PercentChange(decimal current, decimal previous)
        => previous == 0 ? null : Math.Round((current - previous) / previous * 100, 1, MidpointRounding.AwayFromZero);
}
