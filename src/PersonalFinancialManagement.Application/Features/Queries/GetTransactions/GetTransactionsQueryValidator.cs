using FluentValidation;

namespace PersonalFinancialManagement.Application.Features.Queries.GetTransactions;

public class GetTransactionsQueryValidator : AbstractValidator<GetTransactionsQuery>
{
    public GetTransactionsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Type).IsInEnum().When(x => x.Type is not null);
        RuleFor(x => x.Search).MaximumLength(100);

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("'To' must not be earlier than 'From'.");
    }
}
