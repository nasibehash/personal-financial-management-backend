namespace PersonalFinancialManagement.Domain.Common;

// Base class for all entities. Audit timestamps are set automatically by ApplicationDbContext.
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
