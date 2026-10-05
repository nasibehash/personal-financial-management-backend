using System.Globalization;
using PersonalFinancialManagement.Application.Common.Models;

namespace PersonalFinancialManagement.Infrastructure.Services.Ai;

// Offline fallback for the language model: a handful of simple checks written as Persian sentences.
public class RuleBasedInsightGenerator
{
    private const int MaxInsights = 5;

    public IReadOnlyList<string> Generate(FinancialSnapshot s)
    {
        if (s.TotalIncome == 0 && s.TotalExpense == 0)
            return ["در این بازه هنوز تراکنشی ثبت نشده است. با ثبت درآمدها و هزینه‌ها، تحلیل دقیق‌تری دریافت می‌کنید."];

        var insights = new List<string>();

        if (s.TotalExpense > s.TotalIncome)
        {
            insights.Add($"هزینه‌های شما در این بازه {Money(s.TotalExpense - s.TotalIncome)} بیشتر از درآمدتان بوده است. بهتر است هزینه‌های غیرضروری را کاهش دهید.");
        }
        else if (s.TotalIncome > 0)
        {
            var rate = (s.TotalIncome - s.TotalExpense) / s.TotalIncome * 100;
            if (rate >= 20)
                insights.Add($"عالی! حدود {rate:0}٪ از درآمدتان را پس‌انداز کرده‌اید.");
            else if (rate >= 10)
                insights.Add($"نرخ پس‌انداز شما حدود {rate:0}٪ است. هدف رایج، کنار گذاشتن ۲۰٪ از درآمد است.");
            else
                insights.Add($"نرخ پس‌انداز شما فقط حدود {rate:0}٪ است. تلاش کنید حداقل ۱۰ تا ۲۰ درصد درآمد را کنار بگذارید.");
        }

        var top = s.TopExpenseCategories.FirstOrDefault();
        if (top is not null && s.TotalExpense > 0 && top.Percentage >= 40)
            insights.Add($"دسته «{top.Name}» حدود {top.Percentage:0}٪ از کل هزینه‌های شما را تشکیل می‌دهد؛ بررسی این دسته بیشترین اثر را در صرفه‌جویی دارد.");

        if (s.PreviousExpense > 0)
        {
            var change = (s.TotalExpense - s.PreviousExpense) / s.PreviousExpense * 100;
            if (change >= 20)
                insights.Add($"هزینه‌های شما نسبت به دوره قبل {change:0}٪ افزایش یافته است.");
            else if (change <= -20)
                insights.Add($"هزینه‌های شما نسبت به دوره قبل {Math.Abs(change):0}٪ کاهش یافته است؛ ادامه دهید!");
        }

        foreach (var goal in s.Goals.Where(g => g.DaysLeft is >= 0 and <= 30 && g.ProgressPercent < 100).Take(2))
            insights.Add($"موعد هدف «{goal.Name}» نزدیک است ({goal.DaysLeft} روز مانده) و تا اینجا {goal.ProgressPercent:0}٪ پیشرفت داشته‌اید.");

        foreach (var goal in s.Goals.Where(g => g.RequiredMonthlySaving is > 0).Take(2))
            insights.Add($"برای رسیدن به هدف «{goal.Name}» باید ماهانه حدود {Money(goal.RequiredMonthlySaving!.Value)} پس‌انداز کنید.");

        if (insights.Count == 0)
            insights.Add("درآمد و هزینه‌های شما در این بازه متعادل است؛ با پیگیری منظم، اهداف مالی خود را سریع‌تر محقق کنید.");

        return insights.Take(MaxInsights).ToList();
    }

    private static string Money(decimal value) => value.ToString("N0", CultureInfo.InvariantCulture);
}
