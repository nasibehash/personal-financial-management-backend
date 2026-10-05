using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Common.Helpers;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;

namespace PersonalFinancialManagement.Application.Features.Queries.GetTransactionById;

public class GetTransactionByIdQueryHandler : IRequestHandler<GetTransactionByIdQuery, TransactionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetTransactionByIdQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TransactionDto> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        return await _db.Transactions
                   .AsNoTracking()
                   .Where(t => t.Id == request.Id && t.UserId == userId)
                   .ProjectToDto()
                   .FirstOrDefaultAsync(cancellationToken)
               ?? throw new NotFoundException("Transaction", request.Id);
    }
}
