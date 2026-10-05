using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Common.Helpers;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;

namespace PersonalFinancialManagement.Application.Features.Commands.DeleteGoalContribution;

public class DeleteGoalContributionCommandHandler : IRequestHandler<DeleteGoalContributionCommand, GoalDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public DeleteGoalContributionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<GoalDto> Handle(DeleteGoalContributionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var now = _clock.UtcNow;

        var goal = await _db.Goals.FirstOrDefaultAsync(g => g.Id == request.GoalId && g.UserId == userId, cancellationToken)
                   ?? throw new NotFoundException("Goal", request.GoalId);

        var contribution = await _db.GoalContributions
                               .FirstOrDefaultAsync(c => c.Id == request.ContributionId && c.GoalId == goal.Id, cancellationToken)
                           ?? throw new NotFoundException("Contribution", request.ContributionId);

        var total = await _db.GoalContributions
            .Where(c => c.GoalId == goal.Id)
            .SumAsync(c => c.Amount, cancellationToken);

        _db.GoalContributions.Remove(contribution);

        var current = total - contribution.Amount;
        GoalProgress.Reevaluate(goal, current, now);

        await _db.SaveChangesAsync(cancellationToken);

        return GoalProgress.ToDto(goal, current, now);
    }
}
