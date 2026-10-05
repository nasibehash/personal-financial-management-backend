using FluentValidation;

namespace PersonalFinancialManagement.Application.Features.Queries.GetFinancialSummary;

public class GetFinancialSummaryQueryValidator : AbstractValidator<GetFinancialSummaryQuery>
{
    public GetFinancialSummaryQueryValidator()
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("'To' must not be earlier than 'From'.");
    }
}
