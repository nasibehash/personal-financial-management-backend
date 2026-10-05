using MediatR;

namespace PersonalFinancialManagement.Application.Features.Commands.DeleteCategory;

public record DeleteCategoryCommand(Guid Id) : IRequest;
