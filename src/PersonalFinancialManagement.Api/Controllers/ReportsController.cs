using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Features.Queries.GetCategoryBreakdown;
using PersonalFinancialManagement.Application.Features.Queries.GetFinancialSummary;
using PersonalFinancialManagement.Application.Features.Queries.GetMonthlyTrend;
using PersonalFinancialManagement.Application.Features.Queries.GetPeriodComparison;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Api.Controllers;

// Without from/to every report covers the current month up to today.
[ApiController]
[Authorize]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportsController(ISender sender) => _sender = sender;

    // GET api/reports/summary?from=2026-10-01&to=2026-10-31
    [HttpGet("summary")]
    public async Task<ActionResult<FinancialSummaryDto>> Summary(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetFinancialSummaryQuery(from, to), cancellationToken));

    // GET api/reports/category-breakdown?type=Expense&from=...&to=...
    [HttpGet("category-breakdown")]
    public async Task<ActionResult<CategoryBreakdownDto>> CategoryBreakdown(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken,
        [FromQuery] CategoryType type = CategoryType.Expense)
        => Ok(await _sender.Send(new GetCategoryBreakdownQuery(from, to, type), cancellationToken));

    // GET api/reports/monthly-trend?months=6
    [HttpGet("monthly-trend")]
    public async Task<ActionResult<IReadOnlyList<MonthlyTrendItemDto>>> MonthlyTrend(
        CancellationToken cancellationToken, [FromQuery] int months = 6)
        => Ok(await _sender.Send(new GetMonthlyTrendQuery(months), cancellationToken));

    // GET api/reports/comparison?from=...&to=...
    [HttpGet("comparison")]
    public async Task<ActionResult<PeriodComparisonDto>> Comparison(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetPeriodComparisonQuery(from, to), cancellationToken));
}
