using MediatR;
using PersonalFinancialManagement.Application.DTOs;

namespace PersonalFinancialManagement.Application.Features.Queries.GetPeriodComparison;

public record GetPeriodComparisonQuery(DateTime? From = null, DateTime? To = null) : IRequest<PeriodComparisonDto>;
