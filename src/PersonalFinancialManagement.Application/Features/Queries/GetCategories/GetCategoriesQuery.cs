using MediatR;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Features.Queries.GetCategories;

// Type = null returns both income and expense categories.
public record GetCategoriesQuery(CategoryType? Type = null) : IRequest<IReadOnlyList<CategoryDto>>;
