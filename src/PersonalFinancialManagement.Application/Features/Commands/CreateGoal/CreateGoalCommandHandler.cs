using MediatR;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Common.Extensions;
using PersonalFinancialManagement.Application.Common.Helpers;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Domain.Entities;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Features.Commands.CreateGoal;

public class CreateGoalCommandHandler : IRequestHandler<CreateGoalCommand, GoalDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public CreateGoalCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<GoalDto> Handle(CreateGoalCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var now = _clock.UtcNow;

        var startDate = (request.StartDate ?? now).ToUtc().Date;
        var deadline = request.Deadline?.ToUtc().Date;
        if (deadline is { } d && d <= startDate)
            throw new BusinessRuleException("The deadline must be after the start date.");

        var goal = new Goal
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            TargetAmount = Math.Round(request.TargetAmount, 2, MidpointRounding.AwayFromZero),
            StartDate = startDate,
            Deadline = deadline,
            Status = GoalStatus.Active
        };

        _db.Goals.Add(goal);
        await _db.SaveChangesAsync(cancellationToken);

        return GoalProgress.ToDto(goal, 0, now);
    }
}
