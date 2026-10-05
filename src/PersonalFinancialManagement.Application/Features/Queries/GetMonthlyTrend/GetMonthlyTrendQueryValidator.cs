using FluentValidation;

namespace PersonalFinancialManagement.Application.Features.Queries.GetMonthlyTrend;

public class GetMonthlyTrendQueryValidator : AbstractValidator<GetMonthlyTrendQuery>
{
    public GetMonthlyTrendQueryValidator()
    {
        RuleFor(x => x.Months).InclusiveBetween(1, 36);
    }
}
