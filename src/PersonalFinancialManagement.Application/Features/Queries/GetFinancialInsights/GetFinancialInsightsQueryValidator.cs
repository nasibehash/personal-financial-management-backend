using FluentValidation;

namespace PersonalFinancialManagement.Application.Features.Queries.GetFinancialInsights;

public class GetFinancialInsightsQueryValidator : AbstractValidator<GetFinancialInsightsQuery>
{
    public GetFinancialInsightsQueryValidator()
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("'To' must not be earlier than 'From'.");
    }
}
