using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PersonalFinancialManagement.Application.Common;
using PersonalFinancialManagement.Domain.Enums;
using PersonalFinancialManagement.Infrastructure.Options;
using PersonalFinancialManagement.Infrastructure.Services.Ai;
using PersonalFinancialManagement.Tests.Support;

namespace PersonalFinancialManagement.Tests.Ai;

public class LlmParserTests
{
    private static LlmTransactionTextParser Parser(FakeLlmClient llm) => new(llm);

    [Fact]
    public async Task A_valid_reply_becomes_a_parsed_transaction()
    {
        var llm = new FakeLlmClient("""
            {"type":"Expense","amount":250000,"category":"خوراک","account":"saman","description":"ناهار","date":"2026-10-14"}
            """);

        var result = await Parser(llm).ParseAsync("دیروز ناهار ۲۵۰ هزار از سامان", AiTestData.Context(), default);

        Assert.NotNull(result);
        Assert.Equal(TransactionType.Expense, result.Type);
        Assert.Equal(250_000, result.Amount);
        Assert.Equal(DefaultCategories.Food, result.CategoryName);
        Assert.Equal("Saman", result.AccountName); // matched case-insensitively to the user's account
        Assert.Equal("ناهار", result.Description);
        Assert.Equal(new DateTime(2026, 10, 14), result.Date);
        Assert.Equal("ai", result.Method);
    }

    [Fact]
    public async Task The_prompt_carries_the_text_the_date_and_the_users_names()
    {
        var llm = new FakeLlmClient("""{"type":null}""");

        await Parser(llm).ParseAsync("خرید نان", AiTestData.Context(), default);

        using var payload = JsonDocument.Parse(llm.LastUserPrompt!);
        Assert.Equal("خرید نان", payload.RootElement.GetProperty("text").GetString());
        Assert.Equal("2026-10-15", payload.RootElement.GetProperty("currentDate").GetString());
        Assert.Contains(DefaultCategories.Food, payload.RootElement.GetProperty("expenseCategories").EnumerateArray().Select(e => e.GetString()));
        Assert.Contains("Saman", payload.RootElement.GetProperty("accounts").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task Names_the_model_invented_are_dropped()
    {
        var llm = new FakeLlmClient("""{"type":"Expense","amount":100,"category":"Made up","account":"Nowhere"}""");

        var result = await Parser(llm).ParseAsync("x 100", AiTestData.Context(), default);

        Assert.Null(result!.CategoryName);
        Assert.Null(result.AccountName);
    }

    [Fact]
    public async Task A_category_of_the_other_type_is_not_accepted()
    {
        var llm = new FakeLlmClient($$"""{"type":"Expense","amount":100,"category":"{{DefaultCategories.Salary}}"}""");

        var result = await Parser(llm).ParseAsync("x 100", AiTestData.Context(), default);

        Assert.Null(result!.CategoryName);
    }

    [Fact]
    public async Task A_null_type_means_the_text_is_not_a_transaction()
    {
        var llm = new FakeLlmClient("""{"type":null}""");

        Assert.Null(await Parser(llm).ParseAsync("سلام", AiTestData.Context(), default));
    }

    [Theory]
    [InlineData("""{"type":"Expense","amount":0}""")]
    [InlineData("""{"type":"Expense","amount":-5}""")]
    [InlineData("""{"type":"Expense"}""")]
    [InlineData("""{"type":"Gift","amount":5}""")]
    public async Task Replies_without_a_usable_type_or_amount_are_not_transactions(string reply)
    {
        Assert.Null(await Parser(new FakeLlmClient(reply)).ParseAsync("x", AiTestData.Context(), default));
    }

    [Fact]
    public async Task The_amount_may_be_a_string_with_persian_digits()
    {
        var llm = new FakeLlmClient("""{"type":"Income","amount":"۵۰,۰۰۰"}""");

        var result = await Parser(llm).ParseAsync("x", AiTestData.Context(), default);

        Assert.Equal(50_000, result!.Amount);
    }

    [Fact]
    public async Task A_transfer_resolves_both_accounts_and_has_no_category()
    {
        var llm = new FakeLlmClient("""
            {"type":"Transfer","amount":1000000,"category":"خوراک","account":"Saman","destinationAccount":"کیف نقدی"}
            """);

        var result = await Parser(llm).ParseAsync("x", AiTestData.Context(), default);

        Assert.Equal(TransactionType.Transfer, result!.Type);
        Assert.Equal("Saman", result.AccountName);
        Assert.Equal("کیف نقدی", result.DestinationAccountName);
        Assert.Null(result.CategoryName);
    }

    [Fact]
    public async Task A_reply_that_is_not_json_is_an_error()
    {
        await Assert.ThrowsAnyAsync<JsonException>(
            () => Parser(new FakeLlmClient("sorry, I can't")).ParseAsync("x", AiTestData.Context(), default));
    }

    private static TransactionTextParser Composite(AiOptions options, FakeLlmClient llm)
        => new(Options.Create(options), Parser(llm), new RuleBasedTransactionTextParser(), NullLogger<TransactionTextParser>.Instance);

    [Fact]
    public async Task Without_an_api_key_the_rules_are_used_and_the_model_is_never_called()
    {
        var llm = new FakeLlmClient("""{"type":"Income","amount":1}""");

        var result = await Composite(new AiOptions(), llm).ParseAsync("خرید نان 20 هزار", AiTestData.Context(), default);

        Assert.Equal("rules", result!.Method);
        Assert.Equal(0, llm.Calls);
    }

    [Fact]
    public async Task With_an_api_key_the_model_result_is_used()
    {
        var llm = new FakeLlmClient("""{"type":"Expense","amount":777}""");

        var result = await Composite(new AiOptions { ApiKey = "key" }, llm).ParseAsync("خرید نان 20 هزار", AiTestData.Context(), default);

        Assert.Equal("ai", result!.Method);
        Assert.Equal(777, result.Amount);
    }

    [Fact]
    public async Task When_the_model_fails_the_rules_take_over()
    {
        var llm = FakeLlmClient.Failing(new HttpRequestException("network down"));

        var result = await Composite(new AiOptions { ApiKey = "key" }, llm).ParseAsync("خرید نان 20 هزار", AiTestData.Context(), default);

        Assert.Equal("rules", result!.Method);
        Assert.Equal(20_000, result.Amount);
    }

    [Fact]
    public async Task When_the_model_says_it_is_not_a_transaction_the_rules_are_not_consulted()
    {
        var llm = new FakeLlmClient("""{"type":null}""");

        var result = await Composite(new AiOptions { ApiKey = "key" }, llm).ParseAsync("خرید نان 20 هزار", AiTestData.Context(), default);

        Assert.Null(result);
    }
}
