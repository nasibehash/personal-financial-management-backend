using FluentValidation;
using PersonalFinancialManagement.Application.Common;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Features.Commands.UpdateAccount;
using PersonalFinancialManagement.Application.Features.Queries.GetCategoryBreakdown;
using PersonalFinancialManagement.Application.Features.Queries.GetFinancialSummary;
using PersonalFinancialManagement.Application.Features.Queries.GetMonthlyTrend;
using PersonalFinancialManagement.Application.Features.Queries.GetPeriodComparison;
using PersonalFinancialManagement.Domain.Enums;
using PersonalFinancialManagement.Tests.Support;

namespace PersonalFinancialManagement.Tests.Features;

// The test clock is fixed at 2026-10-15, so "this month" is 1 to 15 October.
public class ReportTests
{
    private static DateTime Utc(int month, int day) => new(2026, month, day, 12, 0, 0, DateTimeKind.Utc);

    private sealed record Scenario(
        Guid UserId, Guid MainId, Guid SavingsId, Guid FoodId, Guid TransportId, Guid SalaryId);

    private static async Task<Scenario> SetUp(TestApp app, string email = "sara@example.com")
    {
        var user = await app.SignUp(email);
        var main = await app.CreateAccount("Main", 1000);
        var savings = await app.CreateAccount("Savings", 0);
        return new Scenario(
            user.Id,
            main.Id,
            savings.Id,
            await app.CategoryId(DefaultCategories.Food, CategoryType.Expense),
            await app.CategoryId(DefaultCategories.Transport, CategoryType.Expense),
            await app.CategoryId(DefaultCategories.Salary, CategoryType.Income));
    }

    private static Task Add(TestApp app, params PersonalFinancialManagement.Domain.Entities.Transaction[] transactions)
        => app.Execute(db =>
        {
            db.Transactions.AddRange(transactions);
            return Task.CompletedTask;
        });

    [Fact]
    public async Task Summary_covers_the_current_month_by_default_and_ignores_transfers()
    {
        using var app = new TestApp();
        var s = await SetUp(app);
        await Add(app,
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Income, 5000, Utc(10, 3), s.SalaryId),
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Expense, 600, Utc(10, 5), s.FoodId),
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Expense, 400, Utc(10, 10), s.TransportId),
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Transfer, 700, Utc(10, 12), destinationAccountId: s.SavingsId),
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Expense, 999, Utc(9, 20), s.FoodId));

        var summary = await app.Send(new GetFinancialSummaryQuery());

        Assert.Equal(new DateTime(2026, 10, 1), summary.From);
        Assert.Equal(new DateTime(2026, 10, 15), summary.To);
        Assert.Equal(5000, summary.TotalIncome);
        Assert.Equal(1000, summary.TotalExpense);
        Assert.Equal(4000, summary.NetAmount);
        Assert.Equal(80.0m, summary.SavingsRate);
        Assert.Equal(3, summary.TransactionCount);
        Assert.Equal(66.67m, summary.AverageDailyExpense); // 1000 over 15 days
        // 1000 start + 5000 - 600 - 400 - 999; the transfer between two active accounts nets out
        Assert.Equal(4001, summary.TotalBalance);
    }

    [Fact]
    public async Task Summary_uses_an_explicit_range_and_has_no_savings_rate_without_income()
    {
        using var app = new TestApp();
        var s = await SetUp(app);
        await Add(app,
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Expense, 300, Utc(9, 10), s.FoodId),
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Expense, 200, Utc(9, 30), s.FoodId),
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Expense, 50, Utc(10, 1), s.FoodId));

        var summary = await app.Send(new GetFinancialSummaryQuery(new DateTime(2026, 9, 1), new DateTime(2026, 9, 30)));

        Assert.Equal(500, summary.TotalExpense);
        Assert.Equal(0, summary.TotalIncome);
        Assert.Null(summary.SavingsRate);
        Assert.Equal(16.67m, summary.AverageDailyExpense); // 500 over 30 days
    }

    [Fact]
    public async Task Summary_balance_leaves_out_archived_accounts()
    {
        using var app = new TestApp();
        var s = await SetUp(app);
        await Add(app, TestData.Transaction(s.UserId, s.MainId, TransactionType.Transfer, 700, Utc(10, 12), destinationAccountId: s.SavingsId));
        await app.Send(new UpdateAccountCommand(s.SavingsId, "Savings", AccountType.Cash, 0, true));

        var summary = await app.Send(new GetFinancialSummaryQuery());

        Assert.Equal(300, summary.TotalBalance); // only Main: 1000 - 700
    }

    [Fact]
    public async Task Summary_only_counts_the_current_users_transactions()
    {
        using var app = new TestApp();
        var sara = await SetUp(app, "sara@example.com");
        await Add(app, TestData.Transaction(sara.UserId, sara.MainId, TransactionType.Expense, 600, Utc(10, 5), sara.FoodId));
        await app.SignUp("ali@example.com", "Ali Rezaei");

        var summary = await app.Send(new GetFinancialSummaryQuery());

        Assert.Equal(0, summary.TotalExpense);
        Assert.Equal(0, summary.TransactionCount);
        Assert.Equal(0, summary.TotalBalance);
    }

    [Fact]
    public async Task Category_breakdown_ranks_categories_with_their_share()
    {
        using var app = new TestApp();
        var s = await SetUp(app);
        await Add(app,
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Expense, 600, Utc(10, 5), s.FoodId),
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Expense, 100, Utc(10, 6), s.FoodId),
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Expense, 400, Utc(10, 10), s.TransportId),
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Income, 5000, Utc(10, 3), s.SalaryId));

        var breakdown = await app.Send(new GetCategoryBreakdownQuery());

        Assert.Equal(CategoryType.Expense, breakdown.Type);
        Assert.Equal(1100, breakdown.Total);
        Assert.Equal([DefaultCategories.Food, DefaultCategories.Transport], breakdown.Categories.Select(c => c.CategoryName));
        Assert.Equal([700m, 400m], breakdown.Categories.Select(c => c.Total));
        Assert.Equal([63.6m, 36.4m], breakdown.Categories.Select(c => c.Percentage));
        Assert.Equal([2, 1], breakdown.Categories.Select(c => c.TransactionCount));
    }

    [Fact]
    public async Task Category_breakdown_can_report_income_instead()
    {
        using var app = new TestApp();
        var s = await SetUp(app);
        await Add(app,
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Income, 5000, Utc(10, 3), s.SalaryId),
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Expense, 600, Utc(10, 5), s.FoodId));

        var breakdown = await app.Send(new GetCategoryBreakdownQuery(Type: CategoryType.Income));

        var only = Assert.Single(breakdown.Categories);
        Assert.Equal(DefaultCategories.Salary, only.CategoryName);
        Assert.Equal(100m, only.Percentage);
    }

    [Fact]
    public async Task Category_breakdown_is_empty_when_there_are_no_transactions()
    {
        using var app = new TestApp();
        await SetUp(app);

        var breakdown = await app.Send(new GetCategoryBreakdownQuery());

        Assert.Empty(breakdown.Categories);
        Assert.Equal(0, breakdown.Total);
    }

    [Fact]
    public async Task Monthly_trend_lists_every_month_including_empty_ones()
    {
        using var app = new TestApp();
        var s = await SetUp(app);
        await Add(app,
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Income, 100, Utc(9, 2), s.SalaryId),
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Expense, 40, Utc(9, 20), s.FoodId),
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Income, 5000, Utc(10, 3), s.SalaryId),
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Expense, 1000, Utc(10, 5), s.FoodId),
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Transfer, 77, Utc(10, 6), destinationAccountId: s.SavingsId),
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Expense, 999, Utc(7, 1), s.FoodId));

        var trend = await app.Send(new GetMonthlyTrendQuery(3));

        Assert.Equal([(2026, 8), (2026, 9), (2026, 10)], trend.Select(t => (t.Year, t.Month)));
        Assert.Equal(new MonthlyTrendItemDto(2026, 8, 0, 0, 0), trend[0]);
        Assert.Equal(new MonthlyTrendItemDto(2026, 9, 100, 40, 60), trend[1]);
        Assert.Equal(new MonthlyTrendItemDto(2026, 10, 5000, 1000, 4000), trend[2]);
    }

    [Fact]
    public async Task Monthly_trend_crosses_the_year_boundary()
    {
        using var app = new TestApp();
        await SetUp(app);

        var trend = await app.Send(new GetMonthlyTrendQuery(12));

        Assert.Equal(12, trend.Count);
        Assert.Equal((2025, 11), (trend[0].Year, trend[0].Month));
        Assert.Equal((2026, 10), (trend[^1].Year, trend[^1].Month));
    }

    [Fact]
    public async Task Comparison_measures_the_change_against_the_previous_period()
    {
        using var app = new TestApp();
        var s = await SetUp(app);
        await Add(app,
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Income, 5000, Utc(10, 3), s.SalaryId),
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Expense, 1000, Utc(10, 5), s.FoodId),
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Expense, 500, Utc(9, 20), s.FoodId),
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Expense, 999, Utc(9, 10), s.FoodId));

        // current 1-15 Oct; previous is the 15 days before: 16-30 Sep
        var comparison = await app.Send(new GetPeriodComparisonQuery());

        Assert.Equal(new DateTime(2026, 9, 16), comparison.Previous.From);
        Assert.Equal(new DateTime(2026, 9, 30), comparison.Previous.To);
        Assert.Equal(1000, comparison.Current.TotalExpense);
        Assert.Equal(500, comparison.Previous.TotalExpense);
        Assert.Equal(100.0m, comparison.ExpenseChangePercent);
        Assert.Null(comparison.IncomeChangePercent); // there was no income before
    }

    [Fact]
    public async Task Comparison_reports_a_decrease_as_a_negative_percentage()
    {
        using var app = new TestApp();
        var s = await SetUp(app);
        await Add(app,
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Expense, 300, Utc(10, 5), s.FoodId),
            TestData.Transaction(s.UserId, s.MainId, TransactionType.Expense, 400, Utc(9, 20), s.FoodId));

        var comparison = await app.Send(new GetPeriodComparisonQuery());

        Assert.Equal(-25.0m, comparison.ExpenseChangePercent);
    }

    [Fact]
    public async Task Reports_reject_an_inverted_range_and_a_bad_month_count()
    {
        using var app = new TestApp();
        await SetUp(app);
        var from = new DateTime(2026, 10, 20);
        var to = new DateTime(2026, 10, 1);

        await Assert.ThrowsAsync<ValidationException>(() => app.Send(new GetFinancialSummaryQuery(from, to)));
        await Assert.ThrowsAsync<ValidationException>(() => app.Send(new GetCategoryBreakdownQuery(from, to)));
        await Assert.ThrowsAsync<ValidationException>(() => app.Send(new GetPeriodComparisonQuery(from, to)));
        await Assert.ThrowsAsync<ValidationException>(() => app.Send(new GetMonthlyTrendQuery(0)));
        await Assert.ThrowsAsync<ValidationException>(() => app.Send(new GetMonthlyTrendQuery(37)));
    }
}
