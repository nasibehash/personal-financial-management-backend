using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Interfaces;

namespace PersonalFinancialManagement.Application.Features.Commands.DeleteAccount;

public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteAccountCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == request.Id && a.UserId == userId, cancellationToken)
                      ?? throw new NotFoundException("Account", request.Id);

        var hasTransactions = await _db.Transactions.AnyAsync(
            t => t.AccountId == account.Id || t.DestinationAccountId == account.Id, cancellationToken);
        if (hasTransactions)
            throw new ConflictException("The account has transactions and cannot be deleted. Archive it instead.");

        _db.Accounts.Remove(account);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
