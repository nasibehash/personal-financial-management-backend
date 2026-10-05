using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Common;

// Categories every new user starts with. The names are also what the text parser
// maps free-form descriptions onto, so keep them in sync with the parser's keywords.
public static class DefaultCategories
{
    public const string Food = "خوراک";
    public const string Transport = "حمل و نقل";
    public const string Housing = "مسکن";
    public const string Bills = "قبوض";
    public const string Health = "سلامت";
    public const string Education = "آموزش";
    public const string Entertainment = "تفریح";
    public const string Shopping = "خرید";
    public const string Other = "سایر";

    public const string Salary = "حقوق";
    public const string Freelance = "کار آزاد";
    public const string Investment = "سرمایه گذاری";
    public const string Gift = "هدیه";

    public static IReadOnlyList<(string Name, CategoryType Type)> All { get; } =
    [
        (Food, CategoryType.Expense),
        (Transport, CategoryType.Expense),
        (Housing, CategoryType.Expense),
        (Bills, CategoryType.Expense),
        (Health, CategoryType.Expense),
        (Education, CategoryType.Expense),
        (Entertainment, CategoryType.Expense),
        (Shopping, CategoryType.Expense),
        (Other, CategoryType.Expense),
        (Salary, CategoryType.Income),
        (Freelance, CategoryType.Income),
        (Investment, CategoryType.Income),
        (Gift, CategoryType.Income),
        (Other, CategoryType.Income)
    ];
}
