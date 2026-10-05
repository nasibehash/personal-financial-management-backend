using PersonalFinancialManagement.Domain.Common;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Domain.Entities;

// A savings goal. Progress is the sum of its contributions.
public class Goal : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal TargetAmount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? Deadline { get; set; }

    public GoalStatus Status { get; set; } = GoalStatus.Active;

    public DateTime? CompletedAtUtc { get; set; }

    public ICollection<GoalContribution> Contributions { get; set; } = new List<GoalContribution>();
}
