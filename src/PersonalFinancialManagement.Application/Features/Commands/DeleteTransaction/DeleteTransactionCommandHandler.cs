using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Interfaces;

namespace PersonalFinancialManagement.Application.Features.Commands.DeleteTransaction;

public class DeleteTransactionCommandHandler : IRequestHandler<DeleteTransactionCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteTransactionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var transaction = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == userId, cancellationToken)
                          ?? throw new NotFoundException("Transaction", request.Id);

        _db.Transactions.Remove(transaction);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
