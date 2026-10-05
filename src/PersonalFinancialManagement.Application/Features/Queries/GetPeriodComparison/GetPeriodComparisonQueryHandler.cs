using MediatR;
using PersonalFinancialManagement.Application.Common.Helpers;
using PersonalFinancialManagement.Application.Common.Models;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;

namespace PersonalFinancialManagement.Application.Features.Queries.GetPeriodComparison;

public class GetPeriodComparisonQueryHandler : IRequestHandler<GetPeriodComparisonQuery, PeriodComparisonDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetPeriodComparisonQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<PeriodComparisonDto> Handle(GetPeriodComparisonQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var currentRange = DateRange.ForReport(request.From, request.To, _clock.UtcNow);
        var previousRange = currentRange.Previous();

        var current = await ReportQueries.GetTotalsAsync(_db, userId, currentRange, cancellationToken);
        var previous = await ReportQueries.GetTotalsAsync(_db, userId, previousRange, cancellationToken);

        return new PeriodComparisonDto(
            ToDto(currentRange, current),
            ToDto(previousRange, previous),
            ReportQueries.PercentChange(current.Income, previous.Income),
            ReportQueries.PercentChange(current.Expense, previous.Expense));
    }

    private static PeriodTotalsDto ToDto(DateRange range, PeriodTotals totals)
        => new(range.From, range.ToInclusive, totals.Income, totals.Expense, totals.Net);
}
