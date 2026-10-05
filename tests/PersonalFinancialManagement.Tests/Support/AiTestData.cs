using PersonalFinancialManagement.Application.Common;
using PersonalFinancialManagement.Application.Common.Models;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Tests.Support;

public static class AiTestData
{
    public static readonly DateTime Now = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);

    // The default categories plus two accounts, like a freshly registered user would have.
    public static TransactionParsingContext Context(params string[] accountNames)
        => new(
            Now,
            DefaultCategories.All.Where(c => c.Type == CategoryType.Income).Select(c => c.Name).ToList(),
            DefaultCategories.All.Where(c => c.Type == CategoryType.Expense).Select(c => c.Name).ToList(),
            accountNames.Length > 0 ? accountNames : ["کیف نقدی", "Saman"]);
}
