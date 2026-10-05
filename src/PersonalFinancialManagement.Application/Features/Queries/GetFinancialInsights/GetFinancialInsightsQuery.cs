using MediatR;
using PersonalFinancialManagement.Application.DTOs;

namespace PersonalFinancialManagement.Application.Features.Queries.GetFinancialInsights;

// Short, actionable observations about the period (default: the current month so far) based on income,
// spending, the previous period and the user's active goals.
public record GetFinancialInsightsQuery(DateTime? From = null, DateTime? To = null) : IRequest<FinancialInsightsDto>;
