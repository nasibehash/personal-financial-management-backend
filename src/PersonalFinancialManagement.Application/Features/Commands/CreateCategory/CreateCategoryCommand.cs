using MediatR;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Features.Commands.CreateCategory;

public record CreateCategoryCommand(string Name, CategoryType Type) : IRequest<CategoryDto>;
