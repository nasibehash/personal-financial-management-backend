using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Features.Commands.CreateGoal;
using PersonalFinancialManagement.Application.Features.Commands.CreateTransaction;
using PersonalFinancialManagement.Domain.Enums;
using PersonalFinancialManagement.Infrastructure.Persistence;
using PersonalFinancialManagement.Tests.Support;

namespace PersonalFinancialManagement.Tests.Persistence;

public class UtcDateTimeTests
{
    [Fact]
    public void Unspecified_and_local_dates_are_written_as_utc()
    {
        var converter = new UtcDateTimeConverter();
        var unspecified = new DateTime(2026, 10, 5, 9, 30, 0, DateTimeKind.Unspecified);
        var local = new DateTime(2026, 10, 5, 9, 30, 0, DateTimeKind.Local);

        var fromUnspecified = (DateTime)converter.ConvertToProvider(unspecified)!;
        var fromLocal = (DateTime)converter.ConvertToProvider(local)!;

        Assert.Equal(DateTimeKind.Utc, fromUnspecified.Kind);
        Assert.Equal(unspecified.Ticks, fromUnspecified.Ticks); // the wall-clock time is taken as UTC
        Assert.Equal(DateTimeKind.Utc, fromLocal.Kind);
        Assert.Equal(local.ToUniversalTime(), fromLocal);
    }

    [Fact]
    public void Dates_read_from_the_database_are_marked_utc()
    {
        var converter = new UtcDateTimeConverter();

        var read = (DateTime)converter.ConvertFromProvider(new DateTime(2026, 10, 5, 9, 30, 0, DateTimeKind.Unspecified))!;

        Assert.Equal(DateTimeKind.Utc, read.Kind);
    }

    [Fact]
    public void Optional_dates_keep_null_and_are_converted_otherwise()
    {
        var converter = new NullableUtcDateTimeConverter();

        Assert.Null(converter.ConvertToProvider(null));
        Assert.Null(converter.ConvertFromProvider(null));
        var written = (DateTime?)converter.ConvertToProvider(new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Unspecified));
        Assert.Equal(DateTimeKind.Utc, written!.Value.Kind);
    }

    [Fact]
    public async Task Every_date_stored_through_the_application_comes_back_as_utc()
    {
        using var app = new TestApp();
        await app.SignUp();
        var account = await app.CreateAccount();
        var food = await app.CategoryId(PersonalFinancialManagement.Application.Common.DefaultCategories.Food, CategoryType.Expense);

        // dates as model binding produces them: no time zone information
        await app.Send(new CreateTransactionCommand(
            TransactionType.Expense, 10, account.Id, food, Date: new DateTime(2026, 10, 5, 9, 30, 0, DateTimeKind.Unspecified)));
        await app.Send(new CreateGoalCommand("Laptop", 1000, Deadline: new DateTime(2027, 1, 13, 0, 0, 0, DateTimeKind.Unspecified)));

        var kinds = await app.Query(async db => new[]
        {
            (await db.Transactions.AsNoTracking().SingleAsync()).Date.Kind,
            (await db.Goals.AsNoTracking().SingleAsync()).Deadline!.Value.Kind,
            (await db.Goals.AsNoTracking().SingleAsync()).StartDate.Kind,
            (await db.Users.AsNoTracking().SingleAsync()).CreatedAtUtc.Kind
        });

        Assert.All(kinds, kind => Assert.Equal(DateTimeKind.Utc, kind));
    }
}
