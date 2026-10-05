using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Features.Commands.AddTransactionFromText;
using PersonalFinancialManagement.Application.Features.Commands.AddTransactionFromVoice;
using PersonalFinancialManagement.Application.Features.Commands.UpdateAccount;
using PersonalFinancialManagement.Application.Features.Queries.GetAccountById;
using PersonalFinancialManagement.Domain.Enums;
using PersonalFinancialManagement.Tests.Support;

namespace PersonalFinancialManagement.Tests.Features;

public class AiTransactionTests
{
    private static async Task<int> TransactionCount(TestApp app) => await app.Query(db => db.Transactions.CountAsync());

    // ---- text, without an AI provider (built-in rules)

    [Fact]
    public async Task A_sentence_becomes_an_expense_in_the_matching_category()
    {
        using var app = new TestApp();
        await app.SignUp();
        var account = await app.CreateAccount("Main wallet", 100_000);

        var result = await app.Send(new AddTransactionFromTextCommand("خرید نان ۲۰ هزار تومان"));

        var saved = result.Transaction!;
        Assert.Equal(TransactionType.Expense, saved.Type);
        Assert.Equal(20_000, saved.Amount);
        Assert.Equal(DefaultCategories.Food, saved.CategoryName);
        Assert.Equal(TransactionSource.Text, saved.Source);
        Assert.Equal(account.Id, saved.AccountId); // the user's only account
        Assert.Equal("rules", result.Draft.Method);
        Assert.Null(result.Transcript);
        Assert.Equal(80_000, (await app.Send(new GetAccountByIdQuery(account.Id))).Balance);
    }

    [Fact]
    public async Task A_sentence_about_salary_becomes_income()
    {
        using var app = new TestApp();
        await app.SignUp();
        await app.CreateAccount("Main wallet");

        var result = await app.Send(new AddTransactionFromTextCommand("حقوق ۳۰ میلیون دریافت شد"));

        Assert.Equal(TransactionType.Income, result.Transaction!.Type);
        Assert.Equal(30_000_000, result.Transaction.Amount);
        Assert.Equal(DefaultCategories.Salary, result.Transaction.CategoryName);
    }

    [Fact]
    public async Task Unknown_things_end_up_in_the_other_category()
    {
        using var app = new TestApp();
        await app.SignUp();
        await app.CreateAccount();

        var result = await app.Send(new AddTransactionFromTextCommand("کار متفرقه 5000"));

        Assert.Equal(DefaultCategories.Other, result.Transaction!.CategoryName);
    }

    [Fact]
    public async Task Relative_days_in_the_sentence_set_the_date()
    {
        using var app = new TestApp();
        await app.SignUp();
        await app.CreateAccount();

        var result = await app.Send(new AddTransactionFromTextCommand("دیروز ناهار 150000"));

        Assert.Equal(app.Clock.UtcNow.AddDays(-1), result.Transaction!.Date);
    }

    [Fact]
    public async Task A_preview_shows_the_draft_without_saving()
    {
        using var app = new TestApp();
        await app.SignUp();
        var account = await app.CreateAccount("Main wallet");

        var result = await app.Send(new AddTransactionFromTextCommand("خرید نان ۲۰ هزار تومان", Preview: true));

        Assert.Null(result.Transaction);
        Assert.Equal(20_000, result.Draft.Amount);
        Assert.Equal(DefaultCategories.Food, result.Draft.CategoryName);
        Assert.NotNull(result.Draft.CategoryId);
        Assert.Equal(account.Id, result.Draft.AccountId);
        Assert.Equal(0, await TransactionCount(app));
    }

    [Fact]
    public async Task The_account_named_in_the_text_is_used_when_there_are_several()
    {
        using var app = new TestApp();
        await app.SignUp();
        await app.CreateAccount("Saman");
        var cash = await app.CreateAccount("Cash");

        var result = await app.Send(new AddTransactionFromTextCommand("خرید قهوه 90 هزار از Cash"));

        Assert.Equal(cash.Id, result.Transaction!.AccountId);
    }

    [Fact]
    public async Task With_several_accounts_and_none_named_the_caller_must_choose()
    {
        using var app = new TestApp();
        await app.SignUp();
        await app.CreateAccount("Saman");
        var cash = await app.CreateAccount("Cash");

        await Assert.ThrowsAsync<BusinessRuleException>(() => app.Send(new AddTransactionFromTextCommand("خرید نان 20 هزار")));
        Assert.Equal(0, await TransactionCount(app));

        var chosen = await app.Send(new AddTransactionFromTextCommand("خرید نان 20 هزار", cash.Id));
        Assert.Equal(cash.Id, chosen.Transaction!.AccountId);
    }

    [Fact]
    public async Task A_preview_without_a_resolvable_account_still_returns_the_draft()
    {
        using var app = new TestApp();
        await app.SignUp();
        await app.CreateAccount("Saman");
        await app.CreateAccount("Cash");

        var result = await app.Send(new AddTransactionFromTextCommand("خرید نان 20 هزار", Preview: true));

        Assert.Null(result.Draft.AccountId);
        Assert.Null(result.Transaction);
    }

    [Fact]
    public async Task A_user_without_accounts_cannot_save_a_transaction()
    {
        using var app = new TestApp();
        await app.SignUp();

        await Assert.ThrowsAsync<BusinessRuleException>(() => app.Send(new AddTransactionFromTextCommand("خرید نان 20 هزار")));
    }

    [Fact]
    public async Task Text_without_an_amount_is_rejected()
    {
        using var app = new TestApp();
        await app.SignUp();
        await app.CreateAccount();

        await Assert.ThrowsAsync<BusinessRuleException>(() => app.Send(new AddTransactionFromTextCommand("سلام، حال شما چطور است")));
    }

    [Fact]
    public async Task An_account_of_another_user_or_an_archived_one_cannot_be_chosen()
    {
        using var app = new TestApp();
        await app.SignUp("sara@example.com");
        var saraAccount = await app.CreateAccount("Sara");
        await app.SignUp("ali@example.com", "Ali Rezaei");
        var archived = await app.CreateAccount("Old");
        await app.CreateAccount("Current");
        await app.Send(new UpdateAccountCommand(archived.Id, "Old", AccountType.Cash, 0, true));

        await Assert.ThrowsAsync<NotFoundException>(() => app.Send(new AddTransactionFromTextCommand("خرید نان 20 هزار", saraAccount.Id)));
        await Assert.ThrowsAsync<NotFoundException>(() => app.Send(new AddTransactionFromTextCommand("خرید نان 20 هزار", archived.Id)));
    }

    [Fact]
    public async Task Empty_or_oversized_text_is_rejected_by_validation()
    {
        using var app = new TestApp();
        await app.SignUp();

        await Assert.ThrowsAsync<ValidationException>(() => app.Send(new AddTransactionFromTextCommand("  ")));
        await Assert.ThrowsAsync<ValidationException>(() => app.Send(new AddTransactionFromTextCommand(new string('x', 1001))));
    }

    // ---- text, with an AI provider

    [Fact]
    public async Task With_an_api_key_the_language_model_decides()
    {
        using var app = new TestApp();
        await app.SignUp();
        await app.CreateAccount("Main wallet");
        app.Ai.ApiKey = "key";
        app.Llm.Reply = _ => """{"type":"Expense","amount":42000,"category":"سلامت","description":"ویزیت دکتر"}""";

        var result = await app.Send(new AddTransactionFromTextCommand("رفتم دکتر"));

        Assert.Equal("ai", result.Draft.Method);
        Assert.Equal(42_000, result.Transaction!.Amount);
        Assert.Equal(DefaultCategories.Health, result.Transaction.CategoryName);
        Assert.Equal("ویزیت دکتر", result.Transaction.Description);
        Assert.Equal(1, app.Llm.Calls);
    }

    [Fact]
    public async Task The_language_model_can_record_a_transfer()
    {
        using var app = new TestApp();
        await app.SignUp();
        var saman = await app.CreateAccount("Saman", 5_000_000);
        var cash = await app.CreateAccount("Cash");
        app.Ai.ApiKey = "key";
        app.Llm.Reply = _ => """{"type":"Transfer","amount":1000000,"account":"Saman","destinationAccount":"Cash"}""";

        var result = await app.Send(new AddTransactionFromTextCommand("یک میلیون از سامان به کیف نقدی"));

        Assert.Equal(TransactionType.Transfer, result.Transaction!.Type);
        Assert.Equal(TransactionSource.Text, result.Transaction.Source);
        Assert.Equal(4_000_000, (await app.Send(new GetAccountByIdQuery(saman.Id))).Balance);
        Assert.Equal(1_000_000, (await app.Send(new GetAccountByIdQuery(cash.Id))).Balance);
    }

    [Fact]
    public async Task A_transfer_without_a_destination_cannot_be_saved()
    {
        using var app = new TestApp();
        await app.SignUp();
        await app.CreateAccount("Saman", 5_000_000);
        await app.CreateAccount("Cash");
        app.Ai.ApiKey = "key";
        app.Llm.Reply = _ => """{"type":"Transfer","amount":1000,"account":"Saman"}""";

        await Assert.ThrowsAsync<BusinessRuleException>(() => app.Send(new AddTransactionFromTextCommand("انتقال 1000")));
    }

    [Fact]
    public async Task A_failing_language_model_falls_back_to_the_rules()
    {
        using var app = new TestApp();
        await app.SignUp();
        await app.CreateAccount();
        app.Ai.ApiKey = "key";
        app.Llm.Reply = _ => throw new HttpRequestException("network down");

        var result = await app.Send(new AddTransactionFromTextCommand("خرید نان 20 هزار"));

        Assert.Equal("rules", result.Draft.Method);
        Assert.Equal(20_000, result.Transaction!.Amount);
    }

    [Fact]
    public async Task The_model_only_sees_the_current_users_categories_and_accounts()
    {
        using var app = new TestApp();
        await app.SignUp("sara@example.com");
        await app.CreateAccount("Sara private account");
        await app.SignUp("ali@example.com", "Ali Rezaei");
        await app.CreateAccount("Ali account");
        app.Ai.ApiKey = "key";
        app.Llm.Reply = _ => """{"type":"Expense","amount":10}""";

        await app.Send(new AddTransactionFromTextCommand("x 10", Preview: true));

        Assert.Contains("Ali account", app.Llm.LastUserPrompt);
        Assert.DoesNotContain("Sara private account", app.Llm.LastUserPrompt);
    }

    // ---- voice

    private static Stream Audio() => new MemoryStream([1, 2, 3, 4, 5]);

    [Fact]
    public async Task A_recording_is_transcribed_and_saved_as_a_voice_transaction()
    {
        using var app = new TestApp();
        await app.SignUp();
        await app.CreateAccount();
        app.Speech.Transcript = "خرید نان بیست هزار تومان 20000";

        var result = await app.Send(new AddTransactionFromVoiceCommand(Audio(), "note.webm", "audio/webm"));

        Assert.Equal("خرید نان بیست هزار تومان 20000", result.Transcript);
        Assert.Equal(TransactionSource.Voice, result.Transaction!.Source);
        Assert.Equal(20_000, result.Transaction.Amount);
        Assert.Equal("note.webm", app.Speech.LastFileName);
        Assert.Equal("audio/webm", app.Speech.LastContentType);
        Assert.Equal([1, 2, 3, 4, 5], app.Speech.LastAudio);
    }

    [Fact]
    public async Task A_voice_preview_returns_the_transcript_and_draft_without_saving()
    {
        using var app = new TestApp();
        await app.SignUp();
        await app.CreateAccount();
        app.Speech.Transcript = "ناهار 150000";

        var result = await app.Send(new AddTransactionFromVoiceCommand(Audio(), "note.mp3", "audio/mpeg", Preview: true));

        Assert.Equal("ناهار 150000", result.Transcript);
        Assert.Equal(150_000, result.Draft.Amount);
        Assert.Null(result.Transaction);
        Assert.Equal(0, await TransactionCount(app));
    }

    [Fact]
    public async Task A_recording_without_speech_is_rejected()
    {
        using var app = new TestApp();
        await app.SignUp();
        await app.CreateAccount();
        app.Speech.Transcript = "   ";

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => app.Send(new AddTransactionFromVoiceCommand(Audio(), "note.mp3", "audio/mpeg")));
    }

    [Fact]
    public async Task A_speech_service_failure_is_passed_on()
    {
        using var app = new TestApp();
        await app.SignUp();
        await app.CreateAccount();
        app.Speech.Failure = new ExternalServiceException("Voice input is not configured.");

        await Assert.ThrowsAsync<ExternalServiceException>(
            () => app.Send(new AddTransactionFromVoiceCommand(Audio(), "note.mp3", "audio/mpeg")));
    }

    [Theory]
    [InlineData("note.txt")]
    [InlineData("note")]
    [InlineData("")]
    public async Task Unsupported_audio_files_are_rejected_before_transcription(string fileName)
    {
        using var app = new TestApp();
        await app.SignUp();

        await Assert.ThrowsAsync<ValidationException>(
            () => app.Send(new AddTransactionFromVoiceCommand(Audio(), fileName, "audio/mpeg")));
        Assert.Equal(0, app.Speech.Calls);
    }

    [Theory]
    [InlineData("a.mp3")]
    [InlineData("a.M4A")]
    [InlineData("a.wav")]
    [InlineData("a.webm")]
    [InlineData("a.ogg")]
    public async Task Common_audio_formats_are_accepted(string fileName)
    {
        using var app = new TestApp();
        await app.SignUp();
        await app.CreateAccount();
        app.Speech.Transcript = "ناهار 150000";

        var result = await app.Send(new AddTransactionFromVoiceCommand(Audio(), fileName, "audio/mpeg", Preview: true));

        Assert.NotNull(result.Draft);
    }
}
