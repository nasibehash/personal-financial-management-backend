using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Helpers;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;

namespace PersonalFinancialManagement.Application.Features.Queries.GetGoals;

public class GetGoalsQueryHandler : IRequestHandler<GetGoalsQuery, IReadOnlyList<GoalDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetGoalsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<IReadOnlyList<GoalDto>> Handle(GetGoalsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var now = _clock.UtcNow;

        var query = _db.Goals.AsNoTracking().Where(g => g.UserId == userId);
        if (request.Status is { } status)
            query = query.Where(g => g.Status == status);

        var rows = await query
            .Select(g => new { Goal = g, Current = g.Contributions.Sum(c => c.Amount) })
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(r => r.Goal.Status)
            .ThenBy(r => r.Goal.Deadline is null)
            .ThenBy(r => r.Goal.Deadline)
            .ThenBy(r => r.Goal.Name)
            .Select(r => GoalProgress.ToDto(r.Goal, r.Current, now))
            .ToList();
    }
}
