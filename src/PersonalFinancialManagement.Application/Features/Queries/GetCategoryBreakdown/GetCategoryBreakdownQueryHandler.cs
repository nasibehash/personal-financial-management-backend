using MediatR;
using PersonalFinancialManagement.Application.Common.Helpers;
using PersonalFinancialManagement.Application.Common.Models;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Features.Queries.GetCategoryBreakdown;

public class GetCategoryBreakdownQueryHandler : IRequestHandler<GetCategoryBreakdownQuery, CategoryBreakdownDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetCategoryBreakdownQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<CategoryBreakdownDto> Handle(GetCategoryBreakdownQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var range = DateRange.ForReport(request.From, request.To, _clock.UtcNow);
        var transactionType = request.Type == CategoryType.Income ? TransactionType.Income : TransactionType.Expense;

        var totals = await ReportQueries.GetCategoryTotalsAsync(_db, userId, range, transactionType, cancellationToken);
        var grandTotal = totals.Sum(t => t.Total);

        var categories = totals
            .Select(t => new CategorySpendingDto(
                t.CategoryId,
                t.Name,
                t.Total,
                grandTotal == 0 ? 0 : Math.Round(t.Total / grandTotal * 100, 1, MidpointRounding.AwayFromZero),
                t.Count))
            .ToList();

        return new CategoryBreakdownDto(range.From, range.ToInclusive, request.Type, grandTotal, categories);
    }
}
