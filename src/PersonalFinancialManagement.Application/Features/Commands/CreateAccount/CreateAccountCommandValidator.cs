using FluentValidation;

namespace PersonalFinancialManagement.Application.Features.Commands.CreateAccount;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.InitialBalance).GreaterThan(-1_000_000_000_000m).LessThan(1_000_000_000_000m);
    }
}
