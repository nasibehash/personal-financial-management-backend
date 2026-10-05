using MediatR;
using PersonalFinancialManagement.Application.Common.Helpers;
using PersonalFinancialManagement.Application.Common.Models;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;

namespace PersonalFinancialManagement.Application.Features.Queries.GetFinancialSummary;

public class GetFinancialSummaryQueryHandler : IRequestHandler<GetFinancialSummaryQuery, FinancialSummaryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetFinancialSummaryQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<FinancialSummaryDto> Handle(GetFinancialSummaryQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var range = DateRange.ForReport(request.From, request.To, _clock.UtcNow);

        var totals = await ReportQueries.GetTotalsAsync(_db, userId, range, cancellationToken);
        var totalBalance = await ReportQueries.GetTotalBalanceAsync(_db, userId, cancellationToken);

        decimal? savingsRate = totals.Income == 0
            ? null
            : Math.Round(totals.Net / totals.Income * 100, 1, MidpointRounding.AwayFromZero);

        return new FinancialSummaryDto(
            range.From,
            range.ToInclusive,
            totals.Income,
            totals.Expense,
            totals.Net,
            savingsRate,
            totals.TransactionCount,
            Math.Round(totals.Expense / range.Days, 2, MidpointRounding.AwayFromZero),
            totalBalance);
    }
}
