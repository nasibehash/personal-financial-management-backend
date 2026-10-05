using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Common.Extensions;
using PersonalFinancialManagement.Application.Common.Helpers;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Features.Commands.UpdateGoal;

public class UpdateGoalCommandHandler : IRequestHandler<UpdateGoalCommand, GoalDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public UpdateGoalCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<GoalDto> Handle(UpdateGoalCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var now = _clock.UtcNow;

        var goal = await _db.Goals.FirstOrDefaultAsync(g => g.Id == request.Id && g.UserId == userId, cancellationToken)
                   ?? throw new NotFoundException("Goal", request.Id);

        var deadline = request.Deadline?.ToUtc().Date;
        if (deadline is { } d && d <= goal.StartDate.Date)
            throw new BusinessRuleException("The deadline must be after the start date.");

        goal.Name = request.Name.Trim();
        goal.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        goal.TargetAmount = Math.Round(request.TargetAmount, 2, MidpointRounding.AwayFromZero);
        goal.Deadline = deadline;

        var current = await _db.GoalContributions
            .Where(c => c.GoalId == goal.Id)
            .SumAsync(c => c.Amount, cancellationToken);

        if (request.IsCancelled)
        {
            goal.Status = GoalStatus.Cancelled;
        }
        else
        {
            // Reopen a cancelled goal first so it is evaluated against its progress again.
            if (goal.Status == GoalStatus.Cancelled)
                goal.Status = GoalStatus.Active;

            GoalProgress.Reevaluate(goal, current, now);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return GoalProgress.ToDto(goal, current, now);
    }
}
