using MediatR;
using PersonalFinancialManagement.Application.DTOs;

namespace PersonalFinancialManagement.Application.Features.Commands.UpdateCategory;

// Only the name can change; moving a category between income and expense would corrupt its transactions.
public record UpdateCategoryCommand(Guid Id, string Name) : IRequest<CategoryDto>;
