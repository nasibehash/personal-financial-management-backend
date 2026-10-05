using PersonalFinancialManagement.Domain.Common;

namespace PersonalFinancialManagement.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    // Stored trimmed and lower-cased so lookups are case-insensitive.
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
}
