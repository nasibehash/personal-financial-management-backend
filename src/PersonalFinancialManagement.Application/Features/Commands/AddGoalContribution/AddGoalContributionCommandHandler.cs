using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Common.Extensions;
using PersonalFinancialManagement.Application.Common.Helpers;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Domain.Entities;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Features.Commands.AddGoalContribution;

public class AddGoalContributionCommandHandler : IRequestHandler<AddGoalContributionCommand, GoalDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public AddGoalContributionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<GoalDto> Handle(AddGoalContributionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var now = _clock.UtcNow;

        var goal = await _db.Goals.FirstOrDefaultAsync(g => g.Id == request.GoalId && g.UserId == userId, cancellationToken)
                   ?? throw new NotFoundException("Goal", request.GoalId);

        if (goal.Status == GoalStatus.Cancelled)
            throw new BusinessRuleException("A cancelled goal cannot receive contributions. Reopen it first.");

        var amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero);

        var existing = await _db.GoalContributions
            .Where(c => c.GoalId == goal.Id)
            .SumAsync(c => c.Amount, cancellationToken);

        _db.GoalContributions.Add(new GoalContribution
        {
            GoalId = goal.Id,
            Amount = amount,
            Date = (request.Date ?? now).ToUtc(),
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()
        });

        var current = existing + amount;
        GoalProgress.Reevaluate(goal, current, now);

        await _db.SaveChangesAsync(cancellationToken);

        return GoalProgress.ToDto(goal, current, now);
    }
}
