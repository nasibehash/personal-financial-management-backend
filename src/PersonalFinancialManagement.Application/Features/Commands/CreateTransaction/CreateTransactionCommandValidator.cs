using FluentValidation;
using PersonalFinancialManagement.Application.Common.Validation;

namespace PersonalFinancialManagement.Application.Features.Commands.CreateTransaction;

public class CreateTransactionCommandValidator : TransactionInputValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.Source).IsInEnum();
    }
}
