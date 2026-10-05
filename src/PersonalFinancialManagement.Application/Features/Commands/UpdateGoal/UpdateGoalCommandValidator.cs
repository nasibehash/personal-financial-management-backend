using FluentValidation;

namespace PersonalFinancialManagement.Application.Features.Commands.UpdateGoal;

public class UpdateGoalCommandValidator : AbstractValidator<UpdateGoalCommand>
{
    public UpdateGoalCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.TargetAmount).GreaterThanOrEqualTo(0.01m).LessThan(1_000_000_000_000m);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
