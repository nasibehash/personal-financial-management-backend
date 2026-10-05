using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PersonalFinancialManagement.Application.Common.Models;
using PersonalFinancialManagement.Infrastructure.Options;
using PersonalFinancialManagement.Infrastructure.Services.Ai;
using PersonalFinancialManagement.Tests.Support;

namespace PersonalFinancialManagement.Tests.Ai;

public class InsightGeneratorTests
{
    private static readonly DateTime From = new(2026, 10, 1);
    private static readonly DateTime To = new(2026, 10, 31);

    private static FinancialSnapshot Snapshot(
        decimal income = 0,
        decimal expense = 0,
        decimal previousIncome = 0,
        decimal previousExpense = 0,
        IReadOnlyList<CategoryShare>? categories = null,
        IReadOnlyList<GoalSnapshot>? goals = null)
        => new(From, To, income, expense, previousIncome, previousExpense, 0, categories ?? [], goals ?? []);

    private readonly RuleBasedInsightGenerator _rules = new();

    [Fact]
    public void With_no_data_the_user_is_asked_to_record_transactions()
    {
        var insights = _rules.Generate(Snapshot());

        var only = Assert.Single(insights);
        Assert.Contains("تراکنشی ثبت نشده", only);
    }

    [Fact]
    public void Spending_more_than_earning_is_flagged_with_the_gap()
    {
        var insights = _rules.Generate(Snapshot(income: 1_000_000, expense: 1_500_000));

        Assert.Contains(insights, i => i.Contains("500,000") && i.Contains("بیشتر از درآمد"));
    }

    [Theory]
    [InlineData(1_000_000, 700_000, "عالی")]     // 30% saved
    [InlineData(1_000_000, 850_000, "۲۰٪")]      // 15% saved: mentions the usual 20% target
    [InlineData(1_000_000, 950_000, "فقط حدود")] // 5% saved
    public void The_savings_rate_gets_a_matching_message(decimal income, decimal expense, string expected)
    {
        var insights = _rules.Generate(Snapshot(income: income, expense: expense));

        Assert.Contains(insights, i => i.Contains(expected));
    }

    [Fact]
    public void A_dominant_spending_category_is_called_out()
    {
        var insights = _rules.Generate(Snapshot(
            income: 2_000_000,
            expense: 1_000_000,
            categories: [new CategoryShare("خوراک", 600_000, 60), new CategoryShare("حمل و نقل", 400_000, 40)]));

        Assert.Contains(insights, i => i.Contains("«خوراک»") && i.Contains("60"));
    }

    [Fact]
    public void A_large_rise_or_fall_against_the_previous_period_is_reported()
    {
        var rise = _rules.Generate(Snapshot(income: 5_000_000, expense: 1_500_000, previousExpense: 1_000_000));
        var fall = _rules.Generate(Snapshot(income: 5_000_000, expense: 500_000, previousExpense: 1_000_000));
        var steady = _rules.Generate(Snapshot(income: 5_000_000, expense: 1_050_000, previousExpense: 1_000_000));

        Assert.Contains(rise, i => i.Contains("افزایش") && i.Contains("50"));
        Assert.Contains(fall, i => i.Contains("کاهش") && i.Contains("50"));
        Assert.DoesNotContain(steady, i => i.Contains("نسبت به دوره قبل"));
    }

    [Fact]
    public void Goals_near_their_deadline_and_the_monthly_saving_needed_are_mentioned()
    {
        var insights = _rules.Generate(Snapshot(
            income: 5_000_000,
            expense: 1_000_000,
            goals:
            [
                new GoalSnapshot("لپ‌تاپ", 60_000_000, 12_000_000, 20, 12, 3_000_000),
                new GoalSnapshot("سفر", 20_000_000, 2_000_000, 10, 200, 1_500_000)
            ]));

        Assert.Contains(insights, i => i.Contains("«لپ‌تاپ»") && i.Contains("12 روز"));
        Assert.Contains(insights, i => i.Contains("«سفر»") && i.Contains("1,500,000"));
    }

    [Fact]
    public void There_are_never_more_than_five_insights_and_never_none_when_there_is_data()
    {
        var crowded = _rules.Generate(Snapshot(
            income: 1_000_000,
            expense: 1_500_000,
            previousExpense: 500_000,
            categories: [new CategoryShare("خوراک", 1_200_000, 80)],
            goals: Enumerable.Range(1, 6).Select(i => new GoalSnapshot($"هدف {i}", 100, 10, 10, 5, 50)).ToList()));
        var calm = _rules.Generate(Snapshot(income: 5_000_000, expense: 3_900_000, previousExpense: 3_800_000));

        Assert.Equal(5, crowded.Count);
        Assert.NotEmpty(calm);
    }

    private static LlmFinancialInsightGenerator Llm(FakeLlmClient client) => new(client);

    [Fact]
    public async Task The_model_reply_is_turned_into_a_list_of_insights()
    {
        var client = new FakeLlmClient("""{"insights":["اول","  دوم  ",""," ",42]}""");

        var items = await Llm(client).GenerateAsync(Snapshot(income: 10, expense: 5), default);

        Assert.Equal(["اول", "دوم"], items);
    }

    [Fact]
    public async Task The_prompt_contains_the_snapshot_numbers()
    {
        var client = new FakeLlmClient("""{"insights":["x"]}""");

        await Llm(client).GenerateAsync(Snapshot(income: 1234, expense: 567), default);

        using var payload = JsonDocument.Parse(client.LastUserPrompt!);
        Assert.Equal(1234, payload.RootElement.GetProperty("TotalIncome").GetDecimal());
        Assert.Equal(567, payload.RootElement.GetProperty("TotalExpense").GetDecimal());
    }

    [Theory]
    [InlineData("""{"other":1}""")]
    [InlineData("""{"insights":"not an array"}""")]
    [InlineData("""{"insights":[]}""")]
    [InlineData("[]")]
    public async Task A_reply_without_insights_is_an_error(string reply)
    {
        await Assert.ThrowsAnyAsync<JsonException>(
            () => Llm(new FakeLlmClient(reply)).GenerateAsync(Snapshot(income: 10, expense: 5), default));
    }

    private static FinancialInsightGenerator Composite(AiOptions options, FakeLlmClient client)
        => new(Options.Create(options), Llm(client), new RuleBasedInsightGenerator(), NullLogger<FinancialInsightGenerator>.Instance);

    [Fact]
    public async Task Without_an_api_key_rule_based_insights_are_returned()
    {
        var client = new FakeLlmClient("""{"insights":["x"]}""");

        var result = await Composite(new AiOptions(), client).GenerateAsync(Snapshot(income: 10, expense: 5), default);

        Assert.False(result.GeneratedByAi);
        Assert.Equal(0, client.Calls);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public async Task With_an_api_key_the_model_insights_are_returned()
    {
        var client = new FakeLlmClient("""{"insights":["نکته مدل"]}""");

        var result = await Composite(new AiOptions { ApiKey = "key" }, client).GenerateAsync(Snapshot(income: 10, expense: 5), default);

        Assert.True(result.GeneratedByAi);
        Assert.Equal(["نکته مدل"], result.Items);
    }

    [Fact]
    public async Task When_the_model_fails_the_rules_take_over()
    {
        var client = FakeLlmClient.Failing(new HttpRequestException("network down"));

        var result = await Composite(new AiOptions { ApiKey = "key" }, client).GenerateAsync(Snapshot(income: 10, expense: 5), default);

        Assert.False(result.GeneratedByAi);
        Assert.NotEmpty(result.Items);
    }
}
