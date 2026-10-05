using System.Text.Json;
using FluentValidation;
using PersonalFinancialManagement.Application.Common;
using PersonalFinancialManagement.Application.Features.Commands.AddGoalContribution;
using PersonalFinancialManagement.Application.Features.Commands.CreateGoal;
using PersonalFinancialManagement.Application.Features.Queries.GetFinancialInsights;
using PersonalFinancialManagement.Domain.Enums;
using PersonalFinancialManagement.Tests.Support;

namespace PersonalFinancialManagement.Tests.Features;

// The test clock is fixed at 2026-10-15, so the default period is 1 to 15 October.
public class InsightTests
{
    private static DateTime Utc(int month, int day) => new(2026, month, day, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Without_an_api_key_the_rule_based_insights_are_returned()
    {
        using var app = new TestApp();
        var user = await app.SignUp();
        var account = await app.CreateAccount("Main", 0);
        var food = await app.CategoryId(DefaultCategories.Food, CategoryType.Expense);
        var salary = await app.CategoryId(DefaultCategories.Salary, CategoryType.Income);
        await app.Execute(db =>
        {
            db.Transactions.Add(TestData.Transaction(user.Id, account.Id, TransactionType.Income, 1_000_000, Utc(10, 3), salary));
            db.Transactions.Add(TestData.Transaction(user.Id, account.Id, TransactionType.Expense, 1_500_000, Utc(10, 5), food));
            return Task.CompletedTask;
        });

        var result = await app.Send(new GetFinancialInsightsQuery());

        Assert.False(result.GeneratedByAi);
        Assert.Equal(new DateTime(2026, 10, 1), result.From);
        Assert.Equal(new DateTime(2026, 10, 15), result.To);
        Assert.Contains(result.Insights, i => i.Contains("بیشتر از درآمد"));
        Assert.Equal(0, app.Llm.Calls);
    }

    [Fact]
    public async Task A_user_without_transactions_is_asked_to_record_some()
    {
        using var app = new TestApp();
        await app.SignUp();

        var result = await app.Send(new GetFinancialInsightsQuery());

        Assert.Contains("تراکنشی ثبت نشده", Assert.Single(result.Insights));
    }

    [Fact]
    public async Task With_an_api_key_the_model_gets_the_numbers_and_its_insights_are_returned()
    {
        using var app = new TestApp();
        var user = await app.SignUp();
        var account = await app.CreateAccount("Main", 0);
        var food = await app.CategoryId(DefaultCategories.Food, CategoryType.Expense);
        var transport = await app.CategoryId(DefaultCategories.Transport, CategoryType.Expense);
        var salary = await app.CategoryId(DefaultCategories.Salary, CategoryType.Income);
        await app.Execute(db =>
        {
            db.Transactions.Add(TestData.Transaction(user.Id, account.Id, TransactionType.Income, 5000, Utc(10, 3), salary));
            db.Transactions.Add(TestData.Transaction(user.Id, account.Id, TransactionType.Expense, 600, Utc(10, 5), food));
            db.Transactions.Add(TestData.Transaction(user.Id, account.Id, TransactionType.Expense, 400, Utc(10, 6), transport));
            db.Transactions.Add(TestData.Transaction(user.Id, account.Id, TransactionType.Expense, 500, Utc(9, 20), food)); // previous period
            return Task.CompletedTask;
        });

        var active = await app.Send(new CreateGoalCommand("Laptop", 1000, Deadline: new DateTime(2027, 1, 13)));
        await app.Send(new AddGoalContributionCommand(active.Id, 400));
        var done = await app.Send(new CreateGoalCommand("Done", 100));
        await app.Send(new AddGoalContributionCommand(done.Id, 100));

        app.Ai.ApiKey = "key";
        app.Llm.Reply = _ => """{"insights":["نکته مدل"]}""";

        var result = await app.Send(new GetFinancialInsightsQuery());

        Assert.True(result.GeneratedByAi);
        Assert.Equal(["نکته مدل"], result.Insights);

        using var snapshot = JsonDocument.Parse(app.Llm.LastUserPrompt!);
        var root = snapshot.RootElement;
        Assert.Equal(5000, root.GetProperty("TotalIncome").GetDecimal());
        Assert.Equal(1000, root.GetProperty("TotalExpense").GetDecimal());
        Assert.Equal(500, root.GetProperty("PreviousExpense").GetDecimal());
        Assert.Equal(0, root.GetProperty("PreviousIncome").GetDecimal());
        Assert.Equal(3500, root.GetProperty("TotalBalance").GetDecimal()); // 5000 - 1000 - 500

        var categories = root.GetProperty("TopExpenseCategories");
        Assert.Equal(DefaultCategories.Food, categories[0].GetProperty("Name").GetString());
        Assert.Equal(60m, categories[0].GetProperty("Percentage").GetDecimal());

        var goal = Assert.Single(root.GetProperty("Goals").EnumerateArray()); // the completed goal is left out
        Assert.Equal("Laptop", goal.GetProperty("Name").GetString());
        Assert.Equal(40m, goal.GetProperty("ProgressPercent").GetDecimal());
        Assert.Equal(202.92m, goal.GetProperty("RequiredMonthlySaving").GetDecimal());
    }

    [Fact]
    public async Task A_failing_model_falls_back_to_the_rules()
    {
        using var app = new TestApp();
        await app.SignUp();
        app.Ai.ApiKey = "key";
        app.Llm.Reply = _ => throw new HttpRequestException("network down");

        var result = await app.Send(new GetFinancialInsightsQuery());

        Assert.False(result.GeneratedByAi);
        Assert.NotEmpty(result.Insights);
    }

    [Fact]
    public async Task An_inverted_range_is_rejected()
    {
        using var app = new TestApp();
        await app.SignUp();

        await Assert.ThrowsAsync<ValidationException>(
            () => app.Send(new GetFinancialInsightsQuery(new DateTime(2026, 10, 20), new DateTime(2026, 10, 1))));
    }
}
