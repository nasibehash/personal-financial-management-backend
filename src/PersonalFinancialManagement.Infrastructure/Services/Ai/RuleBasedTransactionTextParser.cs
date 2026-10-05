using System.Globalization;
using System.Text.RegularExpressions;
using PersonalFinancialManagement.Application.Common;
using PersonalFinancialManagement.Application.Common.Models;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Infrastructure.Services.Ai;

// Offline fallback for the language model. Understands simple Persian and English sentences such as
// "خرید نان ۲۰ هزار تومان", "حقوق ۳۰ میلیون دریافت شد" or "paid 15k for taxi yesterday".
// It only produces income and expenses; transfers need the language model or manual entry.
public class RuleBasedTransactionTextParser
{
    // A number, optionally followed by a multiplier word ("20 هزار", "15k", "2 million").
    private static readonly Regex AmountPattern = new(
        @"(?<num>\d+(?:\.\d+)?)(?:\s*(?<unit>هزار|میلیون|میلیارد|thousand|million|billion|k|m)(?!\p{L}))?",
        RegexOptions.Compiled);

    private static readonly string[] IncomeWords =
    [
        "حقوق", "درآمد", "دریافت", "واریز", "پاداش", "عیدی", "سود", "فروختم", "فروش",
        "salary", "income", "received", "earned", "deposit", "bonus", "refund"
    ];

    private static readonly string[] ExpenseWords =
    [
        "خرید", "خریدم", "پرداخت", "پرداختم", "هزینه", "دادم", "قبض", "اجاره", "بلیط", "شارژ", "بنزین",
        "expense", "paid", "pay", "bought", "buy", "spent", "purchase"
    ];

    // Keywords that point at one of the default categories.
    private static readonly Dictionary<string, string[]> CategoryKeywords = new()
    {
        [DefaultCategories.Food] = ["غذا", "ناهار", "شام", "صبحانه", "نان", "میوه", "رستوران", "کافه", "قهوه", "سوپرمارکت", "food", "lunch", "dinner", "breakfast", "grocery", "groceries", "restaurant", "coffee", "cafe"],
        [DefaultCategories.Transport] = ["تاکسی", "اسنپ", "تپسی", "بنزین", "مترو", "اتوبوس", "taxi", "uber", "fuel", "gas", "bus", "metro"],
        [DefaultCategories.Housing] = ["اجاره", "رهن", "خانه", "rent"],
        [DefaultCategories.Bills] = ["قبض", "برق", "گاز", "اینترنت", "موبایل", "تلفن", "شارژ", "bill", "electricity", "internet"],
        [DefaultCategories.Health] = ["دارو", "دکتر", "درمان", "بیمارستان", "داروخانه", "pharmacy", "doctor", "medicine"],
        [DefaultCategories.Education] = ["کلاس", "کتاب", "دوره", "دانشگاه", "شهریه", "course", "book", "tuition"],
        [DefaultCategories.Entertainment] = ["سینما", "فیلم", "سفر", "کنسرت", "بازی", "cinema", "movie", "trip", "game"],
        [DefaultCategories.Shopping] = ["لباس", "کفش", "پوشاک", "clothes", "shoes"],
        [DefaultCategories.Salary] = ["حقوق", "salary"],
        [DefaultCategories.Freelance] = ["پروژه", "فریلنس", "freelance", "project"],
        [DefaultCategories.Investment] = ["سود", "سهام", "سرمایه", "dividend", "interest", "stock"],
        [DefaultCategories.Gift] = ["عیدی", "هدیه", "gift"]
    };

    private static readonly HashSet<string> DefaultCategoryNames =
        DefaultCategories.All.Select(c => PersianText.Normalize(c.Name)).ToHashSet();

    private static readonly Dictionary<string, decimal> Multipliers = new()
    {
        ["هزار"] = 1_000m,
        ["میلیون"] = 1_000_000m,
        ["میلیارد"] = 1_000_000_000m,
        ["thousand"] = 1_000m,
        ["k"] = 1_000m,
        ["million"] = 1_000_000m,
        ["m"] = 1_000_000m,
        ["billion"] = 1_000_000_000m
    };

    public ParsedTransaction? Parse(string text, TransactionParsingContext context)
    {
        var normalized = PersianText.Normalize(text);

        var amount = FindAmount(normalized);
        if (amount is null)
            return null;

        var type = DetectType(normalized);
        var categories = type == TransactionType.Income ? context.IncomeCategories : context.ExpenseCategories;

        var description = text.Trim();
        if (description.Length > 500)
            description = description[..500];

        return new ParsedTransaction(
            type,
            amount.Value,
            FindCategory(normalized, categories),
            FindAccount(normalized, context.AccountNames),
            DestinationAccountName: null,
            description,
            FindDate(normalized, context.Now),
            Method: "rules");
    }

    // The largest number in the text wins: in "3 tickets 150000" the price is the amount.
    private static decimal? FindAmount(string normalized)
    {
        decimal? best = null;

        foreach (Match match in AmountPattern.Matches(normalized))
        {
            if (!decimal.TryParse(match.Groups["num"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                continue;

            if (match.Groups["unit"].Success && Multipliers.TryGetValue(match.Groups["unit"].Value, out var multiplier))
                value *= multiplier;

            if (value > 0 && (best is null || value > best))
                best = value;
        }

        return best;
    }

    private static TransactionType DetectType(string normalized)
    {
        var income = IncomeWords.Count(normalized.Contains);
        var expense = ExpenseWords.Count(normalized.Contains);

        // Ties (and no keyword at all) mean an expense, which is by far the most common entry.
        return income > expense ? TransactionType.Income : TransactionType.Expense;
    }

    private static string? FindCategory(string normalized, IReadOnlyList<string> categories)
    {
        // 1. the user mentioned one of their own (non-default) categories
        var custom = categories
            .Where(c => !DefaultCategoryNames.Contains(PersianText.Normalize(c)))
            .Where(c => normalized.Contains(PersianText.Normalize(c)))
            .OrderByDescending(c => c.Length)
            .FirstOrDefault();
        if (custom is not null)
            return custom;

        // 2. a keyword points at one of the default categories the user has
        foreach (var (categoryName, keywords) in CategoryKeywords)
        {
            var existing = categories.FirstOrDefault(c => string.Equals(c, categoryName, StringComparison.OrdinalIgnoreCase));
            if (existing is not null && keywords.Any(normalized.Contains))
                return existing;
        }

        // 3. the text mentions a default category by name. This comes after the keywords because
        // "خرید" (buy) is also the name of the shopping category and appears in most sentences.
        var named = categories
            .Where(c => normalized.Contains(PersianText.Normalize(c)))
            .OrderByDescending(c => c.Length)
            .FirstOrDefault();
        if (named is not null)
            return named;

        // 4. fall back to "Other"
        return categories.FirstOrDefault(c => string.Equals(c, DefaultCategories.Other, StringComparison.OrdinalIgnoreCase));
    }

    private static string? FindAccount(string normalized, IReadOnlyList<string> accountNames)
        => accountNames
            .Where(a => normalized.Contains(PersianText.Normalize(a)))
            .OrderByDescending(a => a.Length)
            .FirstOrDefault();

    private static DateTime? FindDate(string normalized, DateTime now)
    {
        if (normalized.Contains("پریروز"))
            return now.AddDays(-2);

        if (normalized.Contains("دیروز") || normalized.Contains("yesterday"))
            return now.AddDays(-1);

        return null;
    }
}
