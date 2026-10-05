using FluentValidation;

namespace PersonalFinancialManagement.Application.Features.Commands.AddTransactionFromText;

public class AddTransactionFromTextCommandValidator : AbstractValidator<AddTransactionFromTextCommand>
{
    public AddTransactionFromTextCommandValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(1000);
    }
}
