using FluentValidation;

namespace PersonalFinancialManagement.Application.Features.Commands.AddGoalContribution;

public class AddGoalContributionCommandValidator : AbstractValidator<AddGoalContributionCommand>
{
    public AddGoalContributionCommandValidator()
    {
        RuleFor(x => x.GoalId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0.01m).LessThan(1_000_000_000_000m);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
