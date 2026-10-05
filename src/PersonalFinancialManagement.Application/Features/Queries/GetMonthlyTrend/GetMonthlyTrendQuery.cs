using MediatR;
using PersonalFinancialManagement.Application.DTOs;

namespace PersonalFinancialManagement.Application.Features.Queries.GetMonthlyTrend;

// The last N calendar months including the current one, oldest first. Months without data are included as zeros.
public record GetMonthlyTrendQuery(int Months = 6) : IRequest<IReadOnlyList<MonthlyTrendItemDto>>;
