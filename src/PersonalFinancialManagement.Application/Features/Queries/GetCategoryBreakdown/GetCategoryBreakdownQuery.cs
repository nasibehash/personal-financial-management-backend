using MediatR;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Features.Queries.GetCategoryBreakdown;

// Where the money went (Expense) or came from (Income), per category.
public record GetCategoryBreakdownQuery(
    DateTime? From = null,
    DateTime? To = null,
    CategoryType Type = CategoryType.Expense) : IRequest<CategoryBreakdownDto>;
