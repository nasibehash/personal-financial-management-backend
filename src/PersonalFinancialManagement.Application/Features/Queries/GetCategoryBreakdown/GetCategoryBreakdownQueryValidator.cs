using FluentValidation;

namespace PersonalFinancialManagement.Application.Features.Queries.GetCategoryBreakdown;

public class GetCategoryBreakdownQueryValidator : AbstractValidator<GetCategoryBreakdownQuery>
{
    public GetCategoryBreakdownQueryValidator()
    {
        RuleFor(x => x.Type).IsInEnum();

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("'To' must not be earlier than 'From'.");
    }
}
