using PersonalFinancialManagement.Domain.Common;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Domain.Entities;

public class Category : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Name { get; set; } = string.Empty;

    public CategoryType Type { get; set; }
}
