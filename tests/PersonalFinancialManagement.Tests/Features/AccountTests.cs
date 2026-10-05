using FluentValidation;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Features.Commands.CreateAccount;
using PersonalFinancialManagement.Application.Features.Commands.DeleteAccount;
using PersonalFinancialManagement.Application.Features.Commands.UpdateAccount;
using PersonalFinancialManagement.Application.Features.Queries.GetAccountById;
using PersonalFinancialManagement.Application.Features.Queries.GetAccounts;
using PersonalFinancialManagement.Domain.Enums;
using PersonalFinancialManagement.Tests.Support;

namespace PersonalFinancialManagement.Tests.Features;

public class AccountTests
{
    [Fact]
    public async Task Create_starts_with_the_initial_balance()
    {
        using var app = new TestApp();
        await app.SignUp();

        var account = await app.Send(new CreateAccountCommand(" Main wallet ", AccountType.Cash, 1500));

        Assert.Equal("Main wallet", account.Name);
        Assert.Equal(1500, account.Balance);
        Assert.Equal(1500, account.InitialBalance);
        Assert.False(account.IsArchived);
    }

    [Fact]
    public async Task Create_rejects_a_duplicate_name()
    {
        using var app = new TestApp();
        await app.SignUp();
        await app.Send(new CreateAccountCommand("Main wallet", AccountType.Cash));

        await Assert.ThrowsAsync<ConflictException>(() => app.Send(new CreateAccountCommand("Main wallet", AccountType.Card)));
    }

    [Fact]
    public async Task Create_rejects_an_empty_name()
    {
        using var app = new TestApp();
        await app.SignUp();

        await Assert.ThrowsAsync<ValidationException>(() => app.Send(new CreateAccountCommand("", AccountType.Cash)));
    }

    [Fact]
    public async Task Balance_reflects_income_expenses_and_transfers()
    {
        using var app = new TestApp();
        var user = await app.SignUp();
        var a = await app.Send(new CreateAccountCommand("A", AccountType.BankAccount, 1000));
        var b = await app.Send(new CreateAccountCommand("B", AccountType.Cash));
        var now = app.Clock.UtcNow;

        await app.Execute(db =>
        {
            db.Transactions.Add(TestData.Transaction(user.Id, a.Id, TransactionType.Income, 500, now));
            db.Transactions.Add(TestData.Transaction(user.Id, a.Id, TransactionType.Expense, 200, now));
            db.Transactions.Add(TestData.Transaction(user.Id, a.Id, TransactionType.Transfer, 300, now, destinationAccountId: b.Id));
            db.Transactions.Add(TestData.Transaction(user.Id, b.Id, TransactionType.Transfer, 50, now, destinationAccountId: a.Id));
            return Task.CompletedTask;
        });

        var accounts = await app.Send(new GetAccountsQuery());

        // A: 1000 + 500 - 200 - 300 + 50   B: 0 + 300 - 50
        Assert.Equal(1050, accounts.Single(x => x.Id == a.Id).Balance);
        Assert.Equal(250, accounts.Single(x => x.Id == b.Id).Balance);
    }

    [Fact]
    public async Task GetAccounts_hides_archived_accounts_unless_requested()
    {
        using var app = new TestApp();
        await app.SignUp();
        var active = await app.Send(new CreateAccountCommand("Active", AccountType.Cash));
        var old = await app.Send(new CreateAccountCommand("Old", AccountType.Cash));
        await app.Send(new UpdateAccountCommand(old.Id, "Old", AccountType.Cash, 0, true));

        var visible = await app.Send(new GetAccountsQuery());
        var all = await app.Send(new GetAccountsQuery(IncludeArchived: true));

        Assert.Equal([active.Id], visible.Select(x => x.Id));
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetAccounts_only_returns_the_current_users_accounts()
    {
        using var app = new TestApp();
        await app.SignUp("sara@example.com");
        await app.Send(new CreateAccountCommand("Sara wallet", AccountType.Cash));
        await app.SignUp("ali@example.com", "Ali Rezaei");

        Assert.Empty(await app.Send(new GetAccountsQuery()));
    }

    [Fact]
    public async Task GetById_returns_the_account_with_its_balance()
    {
        using var app = new TestApp();
        await app.SignUp();
        var created = await app.Send(new CreateAccountCommand("Main wallet", AccountType.Cash, 75));

        var account = await app.Send(new GetAccountByIdQuery(created.Id));

        Assert.Equal(75, account.Balance);
    }

    [Fact]
    public async Task Another_user_cannot_read_update_or_delete_the_account()
    {
        using var app = new TestApp();
        await app.SignUp("sara@example.com");
        var account = await app.Send(new CreateAccountCommand("Main wallet", AccountType.Cash));
        await app.SignUp("ali@example.com", "Ali Rezaei");

        await Assert.ThrowsAsync<NotFoundException>(() => app.Send(new GetAccountByIdQuery(account.Id)));
        await Assert.ThrowsAsync<NotFoundException>(
            () => app.Send(new UpdateAccountCommand(account.Id, "Mine now", AccountType.Cash, 0, false)));
        await Assert.ThrowsAsync<NotFoundException>(() => app.Send(new DeleteAccountCommand(account.Id)));
    }

    [Fact]
    public async Task Update_changes_the_account_and_returns_the_current_balance()
    {
        using var app = new TestApp();
        var user = await app.SignUp();
        var account = await app.Send(new CreateAccountCommand("Main wallet", AccountType.Cash, 100));
        await app.Execute(db =>
        {
            db.Transactions.Add(TestData.Transaction(user.Id, account.Id, TransactionType.Income, 40, app.Clock.UtcNow));
            return Task.CompletedTask;
        });

        var updated = await app.Send(new UpdateAccountCommand(account.Id, "Savings", AccountType.Savings, 200, false));

        Assert.Equal("Savings", updated.Name);
        Assert.Equal(AccountType.Savings, updated.Type);
        Assert.Equal(240, updated.Balance);
    }

    [Fact]
    public async Task Delete_removes_an_account_without_transactions()
    {
        using var app = new TestApp();
        await app.SignUp();
        var account = await app.Send(new CreateAccountCommand("Main wallet", AccountType.Cash));

        await app.Send(new DeleteAccountCommand(account.Id));

        Assert.Empty(await app.Send(new GetAccountsQuery(IncludeArchived: true)));
    }

    [Fact]
    public async Task Delete_is_refused_when_the_account_is_a_transfer_destination()
    {
        using var app = new TestApp();
        var user = await app.SignUp();
        var from = await app.Send(new CreateAccountCommand("From", AccountType.Cash, 100));
        var to = await app.Send(new CreateAccountCommand("To", AccountType.Cash));
        await app.Execute(db =>
        {
            db.Transactions.Add(TestData.Transaction(user.Id, from.Id, TransactionType.Transfer, 10, app.Clock.UtcNow, destinationAccountId: to.Id));
            return Task.CompletedTask;
        });

        await Assert.ThrowsAsync<ConflictException>(() => app.Send(new DeleteAccountCommand(to.Id)));
    }
}
