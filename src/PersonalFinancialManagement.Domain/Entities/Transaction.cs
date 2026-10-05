using PersonalFinancialManagement.Domain.Common;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Domain.Entities;

// Income and expenses affect one account (AccountId). A transfer moves money from
// AccountId to DestinationAccountId and has no category.
public class Transaction : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid AccountId { get; set; }
    public Account? Account { get; set; }

    public Guid? DestinationAccountId { get; set; }
    public Account? DestinationAccount { get; set; }

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public TransactionType Type { get; set; }

    // Always positive; the direction comes from Type.
    public decimal Amount { get; set; }

    // When the transaction happened (UTC).
    public DateTime Date { get; set; }

    public string? Description { get; set; }

    public TransactionSource Source { get; set; } = TransactionSource.Manual;
}
