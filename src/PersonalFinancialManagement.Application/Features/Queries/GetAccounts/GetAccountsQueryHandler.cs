using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Helpers;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;

namespace PersonalFinancialManagement.Application.Features.Queries.GetAccounts;

public class GetAccountsQueryHandler : IRequestHandler<GetAccountsQuery, IReadOnlyList<AccountDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetAccountsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AccountDto>> Handle(GetAccountsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var query = _db.Accounts.AsNoTracking().Where(a => a.UserId == userId);
        if (!request.IncludeArchived)
            query = query.Where(a => !a.IsArchived);

        var accounts = await query.OrderBy(a => a.Name).ToListAsync(cancellationToken);
        var balances = await AccountBalanceCalculator.CalculateAsync(_db, userId, cancellationToken);

        return accounts.Select(a => a.ToDto(balances[a.Id])).ToList();
    }
}
