using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Common.Helpers;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;

namespace PersonalFinancialManagement.Application.Features.Commands.UpdateAccount;

public class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand, AccountDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateAccountCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AccountDto> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var name = request.Name.Trim();

        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == request.Id && a.UserId == userId, cancellationToken)
                      ?? throw new NotFoundException("Account", request.Id);

        if (await _db.Accounts.AnyAsync(a => a.UserId == userId && a.Name == name && a.Id != account.Id, cancellationToken))
            throw new ConflictException($"An account named '{name}' already exists.");

        account.Name = name;
        account.Type = request.Type;
        account.InitialBalance = request.InitialBalance;
        account.IsArchived = request.IsArchived;
        await _db.SaveChangesAsync(cancellationToken);

        var balances = await AccountBalanceCalculator.CalculateAsync(_db, userId, cancellationToken);
        return account.ToDto(balances[account.Id]);
    }
}
