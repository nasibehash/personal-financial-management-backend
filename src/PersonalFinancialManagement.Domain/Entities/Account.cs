using PersonalFinancialManagement.Domain.Common;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Domain.Entities;

// A wallet or bank account owned by a user. The current balance is derived from
// InitialBalance plus the user's transactions, it is not stored.
public class Account : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; }

    public decimal InitialBalance { get; set; }

    public bool IsArchived { get; set; }
}
