using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Interfaces;

namespace PersonalFinancialManagement.Application.Features.Commands.DeleteCategory;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteCategoryCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == request.Id && c.UserId == userId, cancellationToken)
                       ?? throw new NotFoundException("Category", request.Id);

        var usedBy = await _db.Transactions.CountAsync(t => t.CategoryId == category.Id, cancellationToken);
        if (usedBy > 0)
            throw new ConflictException($"The category is used by {usedBy} transaction(s) and cannot be deleted.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
