using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Extensions;
using PersonalFinancialManagement.Application.Common.Helpers;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Domain.Entities;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Features.Commands.CreateTransaction;

public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, TransactionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public CreateTransactionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<TransactionDto> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var isTransfer = request.Type == TransactionType.Transfer;

        await TransactionReferenceChecker.EnsureValidAsync(
            _db, userId, request.Type, request.AccountId, request.DestinationAccountId, request.CategoryId,
            existing: null, cancellationToken);

        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        var transaction = new Transaction
        {
            UserId = userId,
            AccountId = request.AccountId,
            DestinationAccountId = isTransfer ? request.DestinationAccountId : null,
            CategoryId = isTransfer ? null : request.CategoryId,
            Type = request.Type,
            Amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
            Date = (request.Date ?? _clock.UtcNow).ToUtc(),
            Description = description,
            Source = request.Source
        };

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync(cancellationToken);

        return await _db.Transactions
            .AsNoTracking()
            .Where(t => t.Id == transaction.Id)
            .ProjectToDto()
            .FirstAsync(cancellationToken);
    }
}
