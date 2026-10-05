using FluentValidation;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Features.Commands.AddGoalContribution;
using PersonalFinancialManagement.Application.Features.Commands.CreateGoal;
using PersonalFinancialManagement.Application.Features.Commands.DeleteGoal;
using PersonalFinancialManagement.Application.Features.Commands.DeleteGoalContribution;
using PersonalFinancialManagement.Application.Features.Commands.UpdateGoal;
using PersonalFinancialManagement.Application.Features.Queries.GetGoalById;
using PersonalFinancialManagement.Application.Features.Queries.GetGoals;
using PersonalFinancialManagement.Domain.Enums;
using PersonalFinancialManagement.Tests.Support;

namespace PersonalFinancialManagement.Tests.Features;

// The test clock is fixed at 2026-10-15 12:00 UTC.
public class GoalTests
{
    [Fact]
    public async Task Create_starts_active_with_no_progress()
    {
        using var app = new TestApp();
        await app.SignUp();

        var goal = await app.Send(new CreateGoalCommand("  Laptop ", 1000, Description: " For work "));

        Assert.Equal("Laptop", goal.Name);
        Assert.Equal("For work", goal.Description);
        Assert.Equal(GoalStatus.Active, goal.Status);
        Assert.Equal(0, goal.CurrentAmount);
        Assert.Equal(1000, goal.RemainingAmount);
        Assert.Equal(0, goal.ProgressPercent);
        Assert.Equal(new DateTime(2026, 10, 15), goal.StartDate);
        Assert.Null(goal.Deadline);
        Assert.Null(goal.DaysLeft);
        Assert.Null(goal.RequiredMonthlySaving);
    }

    [Fact]
    public async Task Create_rejects_a_deadline_that_is_not_after_the_start_date()
    {
        using var app = new TestApp();
        await app.SignUp();

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => app.Send(new CreateGoalCommand("Laptop", 1000, Deadline: new DateTime(2026, 10, 15))));
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => app.Send(new CreateGoalCommand("Laptop", 1000, Deadline: new DateTime(2026, 11, 1), StartDate: new DateTime(2026, 11, 5))));
    }

    [Fact]
    public async Task Create_rejects_invalid_input()
    {
        using var app = new TestApp();
        await app.SignUp();

        await Assert.ThrowsAsync<ValidationException>(() => app.Send(new CreateGoalCommand("", 1000)));
        await Assert.ThrowsAsync<ValidationException>(() => app.Send(new CreateGoalCommand("Laptop", 0)));
        await Assert.ThrowsAsync<ValidationException>(() => app.Send(new CreateGoalCommand("Laptop", -5)));
    }

    [Fact]
    public async Task Contributions_move_the_progress_forward()
    {
        using var app = new TestApp();
        await app.SignUp();
        var goal = await app.Send(new CreateGoalCommand("Laptop", 1000));

        await app.Send(new AddGoalContributionCommand(goal.Id, 250, Note: "First"));
        var updated = await app.Send(new AddGoalContributionCommand(goal.Id, 150));

        Assert.Equal(400, updated.CurrentAmount);
        Assert.Equal(600, updated.RemainingAmount);
        Assert.Equal(40.0m, updated.ProgressPercent);
        Assert.Equal(GoalStatus.Active, updated.Status);
    }

    [Fact]
    public async Task Reaching_the_target_completes_the_goal_and_removing_a_contribution_reopens_it()
    {
        using var app = new TestApp();
        await app.SignUp();
        var goal = await app.Send(new CreateGoalCommand("Laptop", 1000));
        await app.Send(new AddGoalContributionCommand(goal.Id, 600));
        var completed = await app.Send(new AddGoalContributionCommand(goal.Id, 500));

        Assert.Equal(GoalStatus.Completed, completed.Status);
        Assert.Equal(100m, completed.ProgressPercent); // capped at 100 even though 1100 was saved
        Assert.Equal(0, completed.RemainingAmount);

        var detail = await app.Send(new GetGoalByIdQuery(goal.Id));
        var last = detail.Contributions.Single(c => c.Amount == 500);
        var reopened = await app.Send(new DeleteGoalContributionCommand(goal.Id, last.Id));

        Assert.Equal(GoalStatus.Active, reopened.Status);
        Assert.Equal(600, reopened.CurrentAmount);
    }

    [Fact]
    public async Task Raising_the_target_of_a_completed_goal_makes_it_active_again()
    {
        using var app = new TestApp();
        await app.SignUp();
        var goal = await app.Send(new CreateGoalCommand("Laptop", 1000));
        await app.Send(new AddGoalContributionCommand(goal.Id, 1000));

        var updated = await app.Send(new UpdateGoalCommand(goal.Id, "Laptop", 1500));

        Assert.Equal(GoalStatus.Active, updated.Status);
        Assert.Equal(500, updated.RemainingAmount);
    }

    [Fact]
    public async Task A_cancelled_goal_rejects_contributions_until_it_is_reopened()
    {
        using var app = new TestApp();
        await app.SignUp();
        var goal = await app.Send(new CreateGoalCommand("Laptop", 1000));
        await app.Send(new AddGoalContributionCommand(goal.Id, 300));

        var cancelled = await app.Send(new UpdateGoalCommand(goal.Id, "Laptop", 1000, IsCancelled: true));
        Assert.Equal(GoalStatus.Cancelled, cancelled.Status);
        await Assert.ThrowsAsync<BusinessRuleException>(() => app.Send(new AddGoalContributionCommand(goal.Id, 100)));

        var reopened = await app.Send(new UpdateGoalCommand(goal.Id, "Laptop", 1000, IsCancelled: false));
        Assert.Equal(GoalStatus.Active, reopened.Status);
        Assert.Equal(300, reopened.CurrentAmount);

        var after = await app.Send(new AddGoalContributionCommand(goal.Id, 100));
        Assert.Equal(400, after.CurrentAmount);
    }

    [Fact]
    public async Task A_goal_with_a_deadline_reports_the_monthly_saving_needed()
    {
        using var app = new TestApp();
        await app.SignUp();
        // 90 days left (15 Oct 2026 -> 13 Jan 2027)
        var goal = await app.Send(new CreateGoalCommand("Laptop", 1000, Deadline: new DateTime(2027, 1, 13)));

        var updated = await app.Send(new AddGoalContributionCommand(goal.Id, 400));

        Assert.Equal(90, updated.DaysLeft);
        Assert.False(updated.IsOverdue);
        Assert.Equal(202.92m, updated.RequiredMonthlySaving); // 600 left over 90 / 30.4375 months
    }

    [Fact]
    public async Task When_the_deadline_is_within_a_month_the_whole_remainder_is_needed()
    {
        using var app = new TestApp();
        await app.SignUp();
        var goal = await app.Send(new CreateGoalCommand("Laptop", 1000, Deadline: new DateTime(2026, 10, 25)));

        var updated = await app.Send(new AddGoalContributionCommand(goal.Id, 400));

        Assert.Equal(10, updated.DaysLeft);
        Assert.Equal(600, updated.RequiredMonthlySaving);
    }

    [Fact]
    public async Task A_goal_past_its_deadline_is_overdue()
    {
        using var app = new TestApp();
        await app.SignUp();
        var goal = await app.Send(new CreateGoalCommand("Laptop", 1000, Deadline: new DateTime(2026, 11, 1)));
        app.Clock.UtcNow = new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

        var overdue = (await app.Send(new GetGoalsQuery())).Single(g => g.Id == goal.Id);

        Assert.True(overdue.IsOverdue);
        Assert.Equal(-9, overdue.DaysLeft);
        Assert.Null(overdue.RequiredMonthlySaving);
    }

    [Fact]
    public async Task GetGoals_filters_by_status_and_puts_the_nearest_deadline_first()
    {
        using var app = new TestApp();
        await app.SignUp();
        var later = await app.Send(new CreateGoalCommand("Later", 100, Deadline: new DateTime(2027, 6, 1)));
        var sooner = await app.Send(new CreateGoalCommand("Sooner", 100, Deadline: new DateTime(2026, 12, 1)));
        var noDeadline = await app.Send(new CreateGoalCommand("No deadline", 100));
        var done = await app.Send(new CreateGoalCommand("Done", 100));
        await app.Send(new AddGoalContributionCommand(done.Id, 100));

        var all = await app.Send(new GetGoalsQuery());
        var completed = await app.Send(new GetGoalsQuery(GoalStatus.Completed));

        Assert.Equal([sooner.Id, later.Id, noDeadline.Id, done.Id], all.Select(g => g.Id));
        Assert.Equal([done.Id], completed.Select(g => g.Id));
    }

    [Fact]
    public async Task GetById_lists_contributions_newest_first()
    {
        using var app = new TestApp();
        await app.SignUp();
        var goal = await app.Send(new CreateGoalCommand("Laptop", 1000));
        await app.Send(new AddGoalContributionCommand(goal.Id, 100, new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)));
        await app.Send(new AddGoalContributionCommand(goal.Id, 200, new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc), "Bonus"));

        var detail = await app.Send(new GetGoalByIdQuery(goal.Id));

        Assert.Equal([200m, 100m], detail.Contributions.Select(c => c.Amount));
        Assert.Equal("Bonus", detail.Contributions[0].Note);
        Assert.Equal(300, detail.Goal.CurrentAmount);
    }

    [Fact]
    public async Task Deleting_a_goal_removes_its_contributions()
    {
        using var app = new TestApp();
        await app.SignUp();
        var goal = await app.Send(new CreateGoalCommand("Laptop", 1000));
        await app.Send(new AddGoalContributionCommand(goal.Id, 100));

        await app.Send(new DeleteGoalCommand(goal.Id));

        await Assert.ThrowsAsync<NotFoundException>(() => app.Send(new GetGoalByIdQuery(goal.Id)));
        var remaining = await app.Query(db => Task.FromResult(db.GoalContributions.Count()));
        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task Another_user_cannot_see_or_change_the_goal()
    {
        using var app = new TestApp();
        await app.SignUp("sara@example.com");
        var goal = await app.Send(new CreateGoalCommand("Laptop", 1000));
        var contribution = (await app.Send(new AddGoalContributionCommand(goal.Id, 100)));
        var contributionId = (await app.Send(new GetGoalByIdQuery(goal.Id))).Contributions.Single().Id;
        await app.SignUp("ali@example.com", "Ali Rezaei");

        Assert.Empty(await app.Send(new GetGoalsQuery()));
        await Assert.ThrowsAsync<NotFoundException>(() => app.Send(new GetGoalByIdQuery(goal.Id)));
        await Assert.ThrowsAsync<NotFoundException>(() => app.Send(new UpdateGoalCommand(goal.Id, "Mine", 1)));
        await Assert.ThrowsAsync<NotFoundException>(() => app.Send(new AddGoalContributionCommand(goal.Id, 5)));
        await Assert.ThrowsAsync<NotFoundException>(() => app.Send(new DeleteGoalContributionCommand(goal.Id, contributionId)));
        await Assert.ThrowsAsync<NotFoundException>(() => app.Send(new DeleteGoalCommand(goal.Id)));
        Assert.Equal(100, contribution.CurrentAmount);
    }

    [Fact]
    public async Task A_contribution_must_belong_to_the_goal_in_the_request()
    {
        using var app = new TestApp();
        await app.SignUp();
        var first = await app.Send(new CreateGoalCommand("First", 1000));
        var second = await app.Send(new CreateGoalCommand("Second", 1000));
        await app.Send(new AddGoalContributionCommand(first.Id, 100));
        var contributionId = (await app.Send(new GetGoalByIdQuery(first.Id))).Contributions.Single().Id;

        await Assert.ThrowsAsync<NotFoundException>(() => app.Send(new DeleteGoalContributionCommand(second.Id, contributionId)));
    }

    [Fact]
    public async Task A_contribution_must_be_positive()
    {
        using var app = new TestApp();
        await app.SignUp();
        var goal = await app.Send(new CreateGoalCommand("Laptop", 1000));

        await Assert.ThrowsAsync<ValidationException>(() => app.Send(new AddGoalContributionCommand(goal.Id, 0)));
        await Assert.ThrowsAsync<ValidationException>(() => app.Send(new AddGoalContributionCommand(goal.Id, -10)));
    }
}
