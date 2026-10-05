using PersonalFinancialManagement.Domain.Common;

namespace PersonalFinancialManagement.Domain.Entities;

public class GoalContribution : BaseEntity
{
    public Guid GoalId { get; set; }
    public Goal? Goal { get; set; }

    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public string? Note { get; set; }
}
