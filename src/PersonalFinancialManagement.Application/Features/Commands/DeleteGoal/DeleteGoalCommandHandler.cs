using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Interfaces;

namespace PersonalFinancialManagement.Application.Features.Commands.DeleteGoal;

public class DeleteGoalCommandHandler : IRequestHandler<DeleteGoalCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteGoalCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteGoalCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var goal = await _db.Goals.FirstOrDefaultAsync(g => g.Id == request.Id && g.UserId == userId, cancellationToken)
                   ?? throw new NotFoundException("Goal", request.Id);

        // Contributions are removed with the goal (cascade delete).
        _db.Goals.Remove(goal);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
