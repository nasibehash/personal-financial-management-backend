using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Features.Queries.GetMonthlyTrend;

public class GetMonthlyTrendQueryHandler : IRequestHandler<GetMonthlyTrendQuery, IReadOnlyList<MonthlyTrendItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetMonthlyTrendQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<IReadOnlyList<MonthlyTrendItemDto>> Handle(GetMonthlyTrendQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var today = _clock.UtcNow.Date;
        var currentMonth = new DateTime(today.Year, today.Month, 1);
        var first = currentMonth.AddMonths(-(request.Months - 1));
        var endExclusive = currentMonth.AddMonths(1);

        var rows = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Date >= first && t.Date < endExclusive && t.Type != TransactionType.Transfer)
            .GroupBy(t => new { t.Date.Year, t.Date.Month, t.Type })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Type, Total = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);

        var result = new List<MonthlyTrendItemDto>(request.Months);
        for (var month = first; month < endExclusive; month = month.AddMonths(1))
        {
            var income = rows.Where(r => r.Year == month.Year && r.Month == month.Month && r.Type == TransactionType.Income).Sum(r => r.Total);
            var expense = rows.Where(r => r.Year == month.Year && r.Month == month.Month && r.Type == TransactionType.Expense).Sum(r => r.Total);
            result.Add(new MonthlyTrendItemDto(month.Year, month.Month, income, expense, income - expense));
        }

        return result;
    }
}
