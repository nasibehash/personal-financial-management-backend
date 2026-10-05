using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Helpers;
using PersonalFinancialManagement.Application.Common.Models;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Features.Queries.GetFinancialInsights;

public class GetFinancialInsightsQueryHandler : IRequestHandler<GetFinancialInsightsQuery, FinancialInsightsDto>
{
    private const int TopCategoryCount = 5;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IFinancialInsightGenerator _generator;

    public GetFinancialInsightsQueryHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IFinancialInsightGenerator generator)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _generator = generator;
    }

    public async Task<FinancialInsightsDto> Handle(GetFinancialInsightsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var now = _clock.UtcNow;
        var range = DateRange.ForReport(request.From, request.To, now);

        var current = await ReportQueries.GetTotalsAsync(_db, userId, range, cancellationToken);
        var previous = await ReportQueries.GetTotalsAsync(_db, userId, range.Previous(), cancellationToken);
        var balance = await ReportQueries.GetTotalBalanceAsync(_db, userId, cancellationToken);

        var categoryTotals = await ReportQueries.GetCategoryTotalsAsync(_db, userId, range, TransactionType.Expense, cancellationToken);
        var topCategories = categoryTotals
            .Take(TopCategoryCount)
            .Select(c => new CategoryShare(
                c.Name,
                c.Total,
                current.Expense == 0 ? 0 : Math.Round(c.Total / current.Expense * 100, 1, MidpointRounding.AwayFromZero)))
            .ToList();

        var goalRows = await _db.Goals
            .AsNoTracking()
            .Where(g => g.UserId == userId && g.Status == GoalStatus.Active)
            .Select(g => new { Goal = g, Current = g.Contributions.Sum(c => c.Amount) })
            .ToListAsync(cancellationToken);

        var goals = goalRows
            .Select(r => GoalProgress.ToDto(r.Goal, r.Current, now))
            .Select(g => new GoalSnapshot(g.Name, g.TargetAmount, g.CurrentAmount, g.ProgressPercent, g.DaysLeft, g.RequiredMonthlySaving))
            .ToList();

        var snapshot = new FinancialSnapshot(
            range.From,
            range.ToInclusive,
            current.Income,
            current.Expense,
            previous.Income,
            previous.Expense,
            balance,
            topCategories,
            goals);

        var insights = await _generator.GenerateAsync(snapshot, cancellationToken);

        return new FinancialInsightsDto(range.From, range.ToInclusive, insights.Items, insights.GeneratedByAi);
    }
}
