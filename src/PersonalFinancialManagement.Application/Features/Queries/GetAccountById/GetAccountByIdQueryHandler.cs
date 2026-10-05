using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Common.Helpers;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;

namespace PersonalFinancialManagement.Application.Features.Queries.GetAccountById;

public class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, AccountDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetAccountByIdQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AccountDto> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var account = await _db.Accounts.AsNoTracking()
                          .FirstOrDefaultAsync(a => a.Id == request.Id && a.UserId == userId, cancellationToken)
                      ?? throw new NotFoundException("Account", request.Id);

        var balances = await AccountBalanceCalculator.CalculateAsync(_db, userId, cancellationToken);
        return account.ToDto(balances[account.Id]);
    }
}
