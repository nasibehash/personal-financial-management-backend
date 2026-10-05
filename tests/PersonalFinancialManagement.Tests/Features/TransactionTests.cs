using FluentValidation;
using PersonalFinancialManagement.Application.Common;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Features.Commands.CreateTransaction;
using PersonalFinancialManagement.Application.Features.Commands.DeleteTransaction;
using PersonalFinancialManagement.Application.Features.Commands.UpdateAccount;
using PersonalFinancialManagement.Application.Features.Commands.UpdateTransaction;
using PersonalFinancialManagement.Application.Features.Queries.GetAccountById;
using PersonalFinancialManagement.Application.Features.Queries.GetTransactionById;
using PersonalFinancialManagement.Application.Features.Queries.GetTransactions;
using PersonalFinancialManagement.Domain.Enums;
using PersonalFinancialManagement.Tests.Support;

namespace PersonalFinancialManagement.Tests.Features;

public class TransactionTests
{
    private static async Task<decimal> BalanceOf(TestApp app, Guid accountId)
        => (await app.Send(new GetAccountByIdQuery(accountId))).Balance;

    [Fact]
    public async Task Creating_an_expense_lowers_the_account_balance()
    {
        using var app = new TestApp();
        await app.SignUp();
        var account = await app.CreateAccount(initialBalance: 1000);
        var food = await app.CategoryId(DefaultCategories.Food, CategoryType.Expense);

        var created = await app.Send(new CreateTransactionCommand(
            TransactionType.Expense, 250, account.Id, food, Description: "  Lunch "));

        Assert.Equal(250, created.Amount);
        Assert.Equal("Lunch", created.Description);
        Assert.Equal(DefaultCategories.Food, created.CategoryName);
        Assert.Equal("Main wallet", created.AccountName);
        Assert.Equal(TransactionSource.Manual, created.Source);
        Assert.Equal(750, await BalanceOf(app, account.Id));
    }

    [Fact]
    public async Task Creating_an_income_raises_the_account_balance()
    {
        using var app = new TestApp();
        await app.SignUp();
        var account = await app.CreateAccount(initialBalance: 100);
        var salary = await app.CategoryId(DefaultCategories.Salary, CategoryType.Income);

        await app.Send(new CreateTransactionCommand(TransactionType.Income, 900, account.Id, salary));

        Assert.Equal(1000, await BalanceOf(app, account.Id));
    }

    [Fact]
    public async Task A_transfer_moves_money_between_accounts()
    {
        using var app = new TestApp();
        await app.SignUp();
        var from = await app.CreateAccount("From", 1000);
        var to = await app.CreateAccount("To", 0);

        var transfer = await app.Send(new CreateTransactionCommand(
            TransactionType.Transfer, 300, from.Id, DestinationAccountId: to.Id));

        Assert.Equal("To", transfer.DestinationAccountName);
        Assert.Null(transfer.CategoryId);
        Assert.Equal(700, await BalanceOf(app, from.Id));
        Assert.Equal(300, await BalanceOf(app, to.Id));
    }

    [Fact]
    public async Task The_date_defaults_to_now()
    {
        using var app = new TestApp();
        await app.SignUp();
        var account = await app.CreateAccount();
        var food = await app.CategoryId(DefaultCategories.Food, CategoryType.Expense);

        var created = await app.Send(new CreateTransactionCommand(TransactionType.Expense, 10, account.Id, food));

        Assert.Equal(app.Clock.UtcNow, created.Date);
    }

    [Fact]
    public async Task The_amount_is_rounded_to_two_decimals()
    {
        using var app = new TestApp();
        await app.SignUp();
        var account = await app.CreateAccount();
        var food = await app.CategoryId(DefaultCategories.Food, CategoryType.Expense);

        var created = await app.Send(new CreateTransactionCommand(TransactionType.Expense, 12.345m, account.Id, food));

        Assert.Equal(12.35m, created.Amount);
    }

    [Fact]
    public async Task Invalid_combinations_are_rejected_by_validation()
    {
        using var app = new TestApp();
        await app.SignUp();
        var account = await app.CreateAccount("A");
        var other = await app.CreateAccount("B");
        var food = await app.CategoryId(DefaultCategories.Food, CategoryType.Expense);

        // amount must be positive
        await Assert.ThrowsAsync<ValidationException>(
            () => app.Send(new CreateTransactionCommand(TransactionType.Expense, 0, account.Id, food)));
        await Assert.ThrowsAsync<ValidationException>(
            () => app.Send(new CreateTransactionCommand(TransactionType.Expense, -5, account.Id, food)));

        // income/expense need a category and cannot have a destination account
        await Assert.ThrowsAsync<ValidationException>(
            () => app.Send(new CreateTransactionCommand(TransactionType.Expense, 10, account.Id)));
        await Assert.ThrowsAsync<ValidationException>(
            () => app.Send(new CreateTransactionCommand(TransactionType.Expense, 10, account.Id, food, other.Id)));

        // transfers need a different destination account and no category
        await Assert.ThrowsAsync<ValidationException>(
            () => app.Send(new CreateTransactionCommand(TransactionType.Transfer, 10, account.Id)));
        await Assert.ThrowsAsync<ValidationException>(
            () => app.Send(new CreateTransactionCommand(TransactionType.Transfer, 10, account.Id, DestinationAccountId: account.Id)));
        await Assert.ThrowsAsync<ValidationException>(
            () => app.Send(new CreateTransactionCommand(TransactionType.Transfer, 10, account.Id, food, other.Id)));
    }

    [Fact]
    public async Task A_category_of_the_wrong_type_is_rejected()
    {
        using var app = new TestApp();
        await app.SignUp();
        var account = await app.CreateAccount();
        var salary = await app.CategoryId(DefaultCategories.Salary, CategoryType.Income);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => app.Send(new CreateTransactionCommand(TransactionType.Expense, 10, account.Id, salary)));
    }

    [Fact]
    public async Task Accounts_and_categories_of_other_users_cannot_be_used()
    {
        using var app = new TestApp();
        await app.SignUp("sara@example.com");
        var saraAccount = await app.CreateAccount("Sara");
        var saraFood = await app.CategoryId(DefaultCategories.Food, CategoryType.Expense);

        await app.SignUp("ali@example.com", "Ali Rezaei");
        var aliAccount = await app.CreateAccount("Ali");
        var aliFood = await app.CategoryId(DefaultCategories.Food, CategoryType.Expense);

        await Assert.ThrowsAsync<NotFoundException>(
            () => app.Send(new CreateTransactionCommand(TransactionType.Expense, 10, saraAccount.Id, aliFood)));
        await Assert.ThrowsAsync<NotFoundException>(
            () => app.Send(new CreateTransactionCommand(TransactionType.Expense, 10, aliAccount.Id, saraFood)));
        await Assert.ThrowsAsync<NotFoundException>(
            () => app.Send(new CreateTransactionCommand(TransactionType.Transfer, 10, aliAccount.Id, DestinationAccountId: saraAccount.Id)));
    }

    [Fact]
    public async Task An_archived_account_cannot_receive_new_transactions()
    {
        using var app = new TestApp();
        await app.SignUp();
        var account = await app.CreateAccount();
        var food = await app.CategoryId(DefaultCategories.Food, CategoryType.Expense);
        await app.Send(new UpdateAccountCommand(account.Id, account.Name, AccountType.Cash, 0, true));

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => app.Send(new CreateTransactionCommand(TransactionType.Expense, 10, account.Id, food)));
    }

    [Fact]
    public async Task Update_replaces_the_fields_and_keeps_the_date_when_none_is_given()
    {
        using var app = new TestApp();
        await app.SignUp();
        var account = await app.CreateAccount(initialBalance: 1000);
        var food = await app.CategoryId(DefaultCategories.Food, CategoryType.Expense);
        var transport = await app.CategoryId(DefaultCategories.Transport, CategoryType.Expense);
        var date = new DateTime(2026, 9, 20, 8, 0, 0, DateTimeKind.Utc);
        var created = await app.Send(new CreateTransactionCommand(TransactionType.Expense, 100, account.Id, food, Date: date));

        var updated = await app.Send(new UpdateTransactionCommand(
            created.Id, TransactionType.Expense, 400, account.Id, transport, Description: "Taxi"));

        Assert.Equal(400, updated.Amount);
        Assert.Equal(DefaultCategories.Transport, updated.CategoryName);
        Assert.Equal("Taxi", updated.Description);
        Assert.Equal(date, updated.Date);
        Assert.Equal(600, await BalanceOf(app, account.Id));
    }

    [Fact]
    public async Task Update_can_turn_an_expense_into_a_transfer()
    {
        using var app = new TestApp();
        await app.SignUp();
        var from = await app.CreateAccount("From", 1000);
        var to = await app.CreateAccount("To");
        var food = await app.CategoryId(DefaultCategories.Food, CategoryType.Expense);
        var created = await app.Send(new CreateTransactionCommand(TransactionType.Expense, 100, from.Id, food));

        var updated = await app.Send(new UpdateTransactionCommand(
            created.Id, TransactionType.Transfer, 100, from.Id, DestinationAccountId: to.Id));

        Assert.Null(updated.CategoryId);
        Assert.Equal(to.Id, updated.DestinationAccountId);
        Assert.Equal(900, await BalanceOf(app, from.Id));
        Assert.Equal(100, await BalanceOf(app, to.Id));
    }

    [Fact]
    public async Task A_transaction_on_an_archived_account_can_still_be_edited_when_the_account_is_unchanged()
    {
        using var app = new TestApp();
        await app.SignUp();
        var account = await app.CreateAccount();
        var food = await app.CategoryId(DefaultCategories.Food, CategoryType.Expense);
        var created = await app.Send(new CreateTransactionCommand(TransactionType.Expense, 100, account.Id, food));
        await app.Send(new UpdateAccountCommand(account.Id, account.Name, AccountType.Cash, 0, true));

        var updated = await app.Send(new UpdateTransactionCommand(
            created.Id, TransactionType.Expense, 100, account.Id, food, Description: "Fixed typo"));

        Assert.Equal("Fixed typo", updated.Description);
    }

    [Fact]
    public async Task Another_user_cannot_read_edit_or_delete_the_transaction()
    {
        using var app = new TestApp();
        await app.SignUp("sara@example.com");
        var account = await app.CreateAccount();
        var food = await app.CategoryId(DefaultCategories.Food, CategoryType.Expense);
        var created = await app.Send(new CreateTransactionCommand(TransactionType.Expense, 100, account.Id, food));
        await app.SignUp("ali@example.com", "Ali Rezaei");

        await Assert.ThrowsAsync<NotFoundException>(() => app.Send(new GetTransactionByIdQuery(created.Id)));
        await Assert.ThrowsAsync<NotFoundException>(() => app.Send(new DeleteTransactionCommand(created.Id)));
        await Assert.ThrowsAsync<NotFoundException>(() => app.Send(new UpdateTransactionCommand(
            created.Id, TransactionType.Expense, 1, account.Id, food)));
    }

    [Fact]
    public async Task Deleting_a_transaction_restores_the_balance()
    {
        using var app = new TestApp();
        await app.SignUp();
        var account = await app.CreateAccount(initialBalance: 500);
        var food = await app.CategoryId(DefaultCategories.Food, CategoryType.Expense);
        var created = await app.Send(new CreateTransactionCommand(TransactionType.Expense, 120, account.Id, food));

        await app.Send(new DeleteTransactionCommand(created.Id));

        Assert.Equal(500, await BalanceOf(app, account.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => app.Send(new GetTransactionByIdQuery(created.Id)));
    }

    [Fact]
    public async Task GetTransactions_filters_by_date_range_with_an_inclusive_end_day()
    {
        using var app = new TestApp();
        await app.SignUp();
        var account = await app.CreateAccount();
        var food = await app.CategoryId(DefaultCategories.Food, CategoryType.Expense);
        foreach (var date in new[]
                 {
                     new DateTime(2026, 9, 30, 23, 0, 0, DateTimeKind.Utc),
                     new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                     new DateTime(2026, 10, 31, 22, 0, 0, DateTimeKind.Utc),
                     new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc)
                 })
        {
            await app.Send(new CreateTransactionCommand(TransactionType.Expense, 10, account.Id, food, Date: date));
        }

        var result = await app.Send(new GetTransactionsQuery(
            From: new DateTime(2026, 10, 1), To: new DateTime(2026, 10, 31)));

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetTransactions_filters_by_type_account_category_and_search()
    {
        using var app = new TestApp();
        await app.SignUp();
        var a = await app.CreateAccount("A", 1000);
        var b = await app.CreateAccount("B");
        var food = await app.CategoryId(DefaultCategories.Food, CategoryType.Expense);
        var transport = await app.CategoryId(DefaultCategories.Transport, CategoryType.Expense);
        var salary = await app.CategoryId(DefaultCategories.Salary, CategoryType.Income);
        await app.Send(new CreateTransactionCommand(TransactionType.Expense, 10, a.Id, food, Description: "Pizza dinner"));
        await app.Send(new CreateTransactionCommand(TransactionType.Expense, 20, b.Id, transport, Description: "Taxi home"));
        await app.Send(new CreateTransactionCommand(TransactionType.Income, 30, b.Id, salary));
        await app.Send(new CreateTransactionCommand(TransactionType.Transfer, 40, a.Id, DestinationAccountId: b.Id));

        Assert.Equal(2, (await app.Send(new GetTransactionsQuery(Type: TransactionType.Expense))).TotalCount);
        Assert.Equal(1, (await app.Send(new GetTransactionsQuery(CategoryId: transport))).TotalCount);
        Assert.Equal(1, (await app.Send(new GetTransactionsQuery(Search: "Pizza"))).TotalCount);

        // account A: its own expense and the outgoing transfer; account B: its two own plus the incoming transfer
        Assert.Equal(2, (await app.Send(new GetTransactionsQuery(AccountId: a.Id))).TotalCount);
        Assert.Equal(3, (await app.Send(new GetTransactionsQuery(AccountId: b.Id))).TotalCount);
    }

    [Fact]
    public async Task GetTransactions_returns_pages_newest_first()
    {
        using var app = new TestApp();
        await app.SignUp();
        var account = await app.CreateAccount();
        var food = await app.CategoryId(DefaultCategories.Food, CategoryType.Expense);
        for (var day = 1; day <= 5; day++)
        {
            await app.Send(new CreateTransactionCommand(
                TransactionType.Expense, day, account.Id, food, Date: new DateTime(2026, 10, day, 12, 0, 0, DateTimeKind.Utc)));
        }

        var firstPage = await app.Send(new GetTransactionsQuery(Page: 1, PageSize: 2));
        var lastPage = await app.Send(new GetTransactionsQuery(Page: 3, PageSize: 2));

        Assert.Equal(5, firstPage.TotalCount);
        Assert.Equal(3, firstPage.TotalPages);
        Assert.Equal([5m, 4m], firstPage.Items.Select(t => t.Amount));
        Assert.Equal([1m], lastPage.Items.Select(t => t.Amount));
    }

    [Fact]
    public async Task GetTransactions_only_returns_the_current_users_transactions()
    {
        using var app = new TestApp();
        await app.SignUp("sara@example.com");
        var account = await app.CreateAccount();
        var food = await app.CategoryId(DefaultCategories.Food, CategoryType.Expense);
        await app.Send(new CreateTransactionCommand(TransactionType.Expense, 10, account.Id, food));
        await app.SignUp("ali@example.com", "Ali Rezaei");

        Assert.Equal(0, (await app.Send(new GetTransactionsQuery())).TotalCount);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetTransactions_rejects_invalid_paging(int page, int pageSize)
    {
        using var app = new TestApp();
        await app.SignUp();

        await Assert.ThrowsAsync<ValidationException>(() => app.Send(new GetTransactionsQuery(Page: page, PageSize: pageSize)));
    }
}
