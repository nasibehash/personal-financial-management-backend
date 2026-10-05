using FluentValidation;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Features.Commands.CreateCategory;
using PersonalFinancialManagement.Application.Features.Commands.DeleteCategory;
using PersonalFinancialManagement.Application.Features.Commands.UpdateCategory;
using PersonalFinancialManagement.Application.Features.Queries.GetCategories;
using PersonalFinancialManagement.Domain.Entities;
using PersonalFinancialManagement.Domain.Enums;
using PersonalFinancialManagement.Tests.Support;

namespace PersonalFinancialManagement.Tests.Features;

public class CategoryTests
{
    [Fact]
    public async Task Create_adds_a_category_for_the_current_user()
    {
        using var app = new TestApp();
        await app.SignUp();

        var created = await app.Send(new CreateCategoryCommand("  Pets ", CategoryType.Expense));

        Assert.Equal("Pets", created.Name);
        var all = await app.Send(new GetCategoriesQuery(CategoryType.Expense));
        Assert.Contains(all, c => c.Id == created.Id);
    }

    [Fact]
    public async Task Create_rejects_a_duplicate_name_of_the_same_type()
    {
        using var app = new TestApp();
        await app.SignUp();
        await app.Send(new CreateCategoryCommand("Pets", CategoryType.Expense));

        await Assert.ThrowsAsync<ConflictException>(() => app.Send(new CreateCategoryCommand("Pets", CategoryType.Expense)));
    }

    [Fact]
    public async Task Create_allows_the_same_name_for_income_and_expense()
    {
        using var app = new TestApp();
        await app.SignUp();
        await app.Send(new CreateCategoryCommand("Refunds", CategoryType.Expense));

        var income = await app.Send(new CreateCategoryCommand("Refunds", CategoryType.Income));

        Assert.Equal(CategoryType.Income, income.Type);
    }

    [Fact]
    public async Task Create_rejects_an_empty_name()
    {
        using var app = new TestApp();
        await app.SignUp();

        await Assert.ThrowsAsync<ValidationException>(() => app.Send(new CreateCategoryCommand("  ", CategoryType.Expense)));
    }

    [Fact]
    public async Task GetCategories_filters_by_type_and_only_returns_the_users_own()
    {
        using var app = new TestApp();
        var sara = await app.SignUp("sara@example.com");
        await app.Send(new CreateCategoryCommand("Sara only", CategoryType.Expense));
        await app.SignUp("ali@example.com", "Ali Rezaei");

        var expenseCategories = await app.Send(new GetCategoriesQuery(CategoryType.Expense));
        var incomeCategories = await app.Send(new GetCategoriesQuery(CategoryType.Income));

        Assert.All(expenseCategories, c => Assert.Equal(CategoryType.Expense, c.Type));
        Assert.All(incomeCategories, c => Assert.Equal(CategoryType.Income, c.Type));
        Assert.DoesNotContain(expenseCategories, c => c.Name == "Sara only");

        app.CurrentUser.Id = sara.Id;
        Assert.Contains(await app.Send(new GetCategoriesQuery()), c => c.Name == "Sara only");
    }

    [Fact]
    public async Task Update_renames_the_category()
    {
        using var app = new TestApp();
        await app.SignUp();
        var created = await app.Send(new CreateCategoryCommand("Pets", CategoryType.Expense));

        var updated = await app.Send(new UpdateCategoryCommand(created.Id, "Animals"));

        Assert.Equal("Animals", updated.Name);
        Assert.Equal(CategoryType.Expense, updated.Type);
    }

    [Fact]
    public async Task Update_rejects_a_name_that_is_already_taken()
    {
        using var app = new TestApp();
        await app.SignUp();
        await app.Send(new CreateCategoryCommand("Pets", CategoryType.Expense));
        var other = await app.Send(new CreateCategoryCommand("Animals", CategoryType.Expense));

        await Assert.ThrowsAsync<ConflictException>(() => app.Send(new UpdateCategoryCommand(other.Id, "Pets")));
    }

    [Fact]
    public async Task Another_user_cannot_update_or_delete_the_category()
    {
        using var app = new TestApp();
        await app.SignUp("sara@example.com");
        var created = await app.Send(new CreateCategoryCommand("Pets", CategoryType.Expense));
        await app.SignUp("ali@example.com", "Ali Rezaei");

        await Assert.ThrowsAsync<NotFoundException>(() => app.Send(new UpdateCategoryCommand(created.Id, "Hacked")));
        await Assert.ThrowsAsync<NotFoundException>(() => app.Send(new DeleteCategoryCommand(created.Id)));
    }

    [Fact]
    public async Task Delete_removes_an_unused_category()
    {
        using var app = new TestApp();
        await app.SignUp();
        var created = await app.Send(new CreateCategoryCommand("Pets", CategoryType.Expense));

        await app.Send(new DeleteCategoryCommand(created.Id));

        Assert.DoesNotContain(await app.Send(new GetCategoriesQuery()), c => c.Id == created.Id);
    }

    [Fact]
    public async Task Delete_is_refused_while_transactions_use_the_category()
    {
        using var app = new TestApp();
        var user = await app.SignUp();
        var category = await app.Send(new CreateCategoryCommand("Pets", CategoryType.Expense));
        await app.Execute(db =>
        {
            var account = new Account { UserId = user.Id, Name = "Cash", Type = AccountType.Cash };
            db.Accounts.Add(account);
            db.Transactions.Add(new Transaction
            {
                UserId = user.Id,
                AccountId = account.Id,
                CategoryId = category.Id,
                Type = TransactionType.Expense,
                Amount = 10,
                Date = app.Clock.UtcNow
            });
            return Task.CompletedTask;
        });

        await Assert.ThrowsAsync<ConflictException>(() => app.Send(new DeleteCategoryCommand(category.Id)));
    }
}
