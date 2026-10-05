using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Common.Helpers;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;

namespace PersonalFinancialManagement.Application.Features.Queries.GetGoalById;

public class GetGoalByIdQueryHandler : IRequestHandler<GetGoalByIdQuery, GoalDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetGoalByIdQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<GoalDetailDto> Handle(GetGoalByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var goal = await _db.Goals.AsNoTracking()
                       .FirstOrDefaultAsync(g => g.Id == request.Id && g.UserId == userId, cancellationToken)
                   ?? throw new NotFoundException("Goal", request.Id);

        var contributions = await _db.GoalContributions
            .AsNoTracking()
            .Where(c => c.GoalId == goal.Id)
            .OrderByDescending(c => c.Date)
            .ThenByDescending(c => c.CreatedAtUtc)
            .Select(c => new GoalContributionDto(c.Id, c.Amount, c.Date, c.Note))
            .ToListAsync(cancellationToken);

        var current = contributions.Sum(c => c.Amount);

        return new GoalDetailDto(GoalProgress.ToDto(goal, current, _clock.UtcNow), contributions);
    }
}
