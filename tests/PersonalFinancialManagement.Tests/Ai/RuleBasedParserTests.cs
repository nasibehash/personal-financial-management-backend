using PersonalFinancialManagement.Application.Common;
using PersonalFinancialManagement.Domain.Enums;
using PersonalFinancialManagement.Infrastructure.Services.Ai;
using PersonalFinancialManagement.Tests.Support;

namespace PersonalFinancialManagement.Tests.Ai;

public class RuleBasedParserTests
{
    private readonly RuleBasedTransactionTextParser _parser = new();

    [Theory]
    [InlineData("خرید نان ۲۰ هزار تومان", 20_000, DefaultCategories.Food)]
    [InlineData("ناهار 150000", 150_000, DefaultCategories.Food)]
    [InlineData("اسنپ 1,250,000 ریال", 1_250_000, DefaultCategories.Transport)]
    [InlineData("قبض برق ۴۵۰۰۰۰", 450_000, DefaultCategories.Bills)]
    [InlineData("دارو ۸۵ هزار", 85_000, DefaultCategories.Health)]
    [InlineData("3 بلیط سینما 450000", 450_000, DefaultCategories.Entertainment)]
    [InlineData("bought lunch 2.5 million", 2_500_000, DefaultCategories.Food)]
    [InlineData("paid 15k for taxi", 15_000, DefaultCategories.Transport)]
    public void Expenses_are_recognised_with_amount_and_category(string text, decimal amount, string category)
    {
        var result = _parser.Parse(text, AiTestData.Context());

        Assert.NotNull(result);
        Assert.Equal(TransactionType.Expense, result.Type);
        Assert.Equal(amount, result.Amount);
        Assert.Equal(category, result.CategoryName);
        Assert.Equal("rules", result.Method);
    }

    [Theory]
    [InlineData("حقوق ۳۰ میلیون دریافت شد", 30_000_000, DefaultCategories.Salary)]
    [InlineData("عیدی ۵ میلیون واریز شد", 5_000_000, DefaultCategories.Gift)]
    [InlineData("received salary 20 million", 20_000_000, DefaultCategories.Salary)]
    public void Income_is_recognised_with_amount_and_category(string text, decimal amount, string category)
    {
        var result = _parser.Parse(text, AiTestData.Context());

        Assert.NotNull(result);
        Assert.Equal(TransactionType.Income, result.Type);
        Assert.Equal(amount, result.Amount);
        Assert.Equal(category, result.CategoryName);
    }

    [Fact]
    public void Persian_digits_and_multiplier_words_are_understood()
    {
        var result = _parser.Parse("خرید ۱۲ میلیون", AiTestData.Context());

        Assert.Equal(12_000_000, result!.Amount);
    }

    [Fact]
    public void A_word_that_only_starts_like_a_multiplier_is_not_one()
    {
        var result = _parser.Parse("waited 50 minutes", AiTestData.Context());

        Assert.Equal(50, result!.Amount);
    }

    [Fact]
    public void Text_without_a_number_is_not_a_transaction()
    {
        Assert.Null(_parser.Parse("امروز هوا خوب است", AiTestData.Context()));
    }

    [Fact]
    public void Unknown_expenses_fall_back_to_the_other_category()
    {
        var result = _parser.Parse("کار متفرقه 5000", AiTestData.Context());

        Assert.Equal(DefaultCategories.Other, result!.CategoryName);
    }

    [Fact]
    public void A_category_named_by_the_user_wins_over_keywords()
    {
        var context = AiTestData.Context() with
        {
            ExpenseCategories = [DefaultCategories.Food, DefaultCategories.Other, "حیوانات خانگی"]
        };

        var result = _parser.Parse("خرید غذای حیوانات خانگی 90 هزار", context);

        Assert.Equal("حیوانات خانگی", result!.CategoryName);
    }

    [Fact]
    public void Categories_the_user_does_not_have_are_not_suggested()
    {
        var context = AiTestData.Context() with { ExpenseCategories = ["Pets"] };

        var result = _parser.Parse("ناهار 100000", context);

        Assert.Null(result!.CategoryName);
    }

    [Fact]
    public void An_account_mentioned_in_the_text_is_picked_up()
    {
        var result = _parser.Parse("خرید قهوه 90 هزار از کیف نقدی", AiTestData.Context());

        Assert.Equal("کیف نقدی", result!.AccountName);
    }

    [Theory]
    [InlineData("دیروز نان خریدم 20 هزار", 1)]
    [InlineData("پریروز نان خریدم 20 هزار", 2)]
    [InlineData("bought bread 20k yesterday", 1)]
    public void Relative_days_are_resolved_from_the_current_date(string text, int daysAgo)
    {
        var result = _parser.Parse(text, AiTestData.Context());

        Assert.Equal(AiTestData.Now.AddDays(-daysAgo), result!.Date);
    }

    [Fact]
    public void Without_a_day_word_the_date_is_left_open()
    {
        var result = _parser.Parse("نان خریدم 20 هزار", AiTestData.Context());

        Assert.Null(result!.Date);
    }

    [Fact]
    public void The_original_text_becomes_the_description()
    {
        var result = _parser.Parse("  خرید نان ۲۰ هزار تومان ", AiTestData.Context());

        Assert.Equal("خرید نان ۲۰ هزار تومان", result!.Description);
    }
}
