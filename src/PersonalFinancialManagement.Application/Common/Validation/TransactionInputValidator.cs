using FluentValidation;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Common.Validation;

public abstract class TransactionInputValidator<T> : AbstractValidator<T> where T : ITransactionInput
{
    protected TransactionInputValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0.01m).LessThan(1_000_000_000_000m);
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(500);

        When(x => x.Type == TransactionType.Transfer, () =>
        {
            RuleFor(x => x.DestinationAccountId)
                .NotNull().WithMessage("A destination account is required for transfers.")
                .NotEqual(x => (Guid?)x.AccountId).WithMessage("The source and destination accounts must be different.");

            RuleFor(x => x.CategoryId).Null().WithMessage("Transfers cannot have a category.");
        }).Otherwise(() =>
        {
            RuleFor(x => x.CategoryId).NotNull().WithMessage("A category is required.");
            RuleFor(x => x.DestinationAccountId).Null().WithMessage("Only transfers can have a destination account.");
        });
    }
}
