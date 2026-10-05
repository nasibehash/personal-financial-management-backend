using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Helpers;
using PersonalFinancialManagement.Application.Common.Models;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;

namespace PersonalFinancialManagement.Application.Features.Queries.GetTransactions;

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, PagedResult<TransactionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetTransactionsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<TransactionDto>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var query = _db.Transactions.AsNoTracking().Where(t => t.UserId == userId);

        if (request.From is { } from)
        {
            var start = from.Date;
            query = query.Where(t => t.Date >= start);
        }

        if (request.To is { } to)
        {
            var endExclusive = to.Date.AddDays(1);
            query = query.Where(t => t.Date < endExclusive);
        }

        if (request.Type is { } type)
            query = query.Where(t => t.Type == type);

        if (request.AccountId is { } accountId)
            query = query.Where(t => t.AccountId == accountId || t.DestinationAccountId == accountId);

        if (request.CategoryId is { } categoryId)
            query = query.Where(t => t.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // Lower-cased on both sides so the search is case-insensitive on every database.
            var term = request.Search.Trim().ToLower();
            query = query.Where(t => t.Description != null && t.Description.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToDto()
            .ToListAsync(cancellationToken);

        return new PagedResult<TransactionDto>(items, request.Page, request.PageSize, totalCount);
    }
}
