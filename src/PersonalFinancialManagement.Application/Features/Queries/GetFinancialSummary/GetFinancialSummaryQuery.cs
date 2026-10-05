using MediatR;
using PersonalFinancialManagement.Application.DTOs;

namespace PersonalFinancialManagement.Application.Features.Queries.GetFinancialSummary;

// From and To are inclusive dates; they default to the first of the current month and today.
public record GetFinancialSummaryQuery(DateTime? From = null, DateTime? To = null) : IRequest<FinancialSummaryDto>;
