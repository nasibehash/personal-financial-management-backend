using FluentValidation;
using PersonalFinancialManagement.Application.Common.Validation;

namespace PersonalFinancialManagement.Application.Features.Commands.UpdateTransaction;

public class UpdateTransactionCommandValidator : TransactionInputValidator<UpdateTransactionCommand>
{
    public UpdateTransactionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
