using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Common.Extensions;
using PersonalFinancialManagement.Application.Common.Helpers;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Features.Commands.UpdateTransaction;

public class UpdateTransactionCommandHandler : IRequestHandler<UpdateTransactionCommand, TransactionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateTransactionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TransactionDto> Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var isTransfer = request.Type == TransactionType.Transfer;

        var transaction = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == userId, cancellationToken)
                          ?? throw new NotFoundException("Transaction", request.Id);

        await TransactionReferenceChecker.EnsureValidAsync(
            _db, userId, request.Type, request.AccountId, request.DestinationAccountId, request.CategoryId,
            existing: transaction, cancellationToken);

        transaction.Type = request.Type;
        transaction.Amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
        transaction.AccountId = request.AccountId;
        transaction.DestinationAccountId = isTransfer ? request.DestinationAccountId : null;
        transaction.CategoryId = isTransfer ? null : request.CategoryId;
        transaction.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (request.Date is { } date)
            transaction.Date = date.ToUtc();

        await _db.SaveChangesAsync(cancellationToken);

        return await _db.Transactions
            .AsNoTracking()
            .Where(t => t.Id == transaction.Id)
            .ProjectToDto()
            .FirstAsync(cancellationToken);
    }
}
