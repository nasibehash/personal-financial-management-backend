using FluentValidation;

namespace PersonalFinancialManagement.Application.Features.Commands.CreateGoal;

public class CreateGoalCommandValidator : AbstractValidator<CreateGoalCommand>
{
    public CreateGoalCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.TargetAmount).GreaterThanOrEqualTo(0.01m).LessThan(1_000_000_000_000m);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
