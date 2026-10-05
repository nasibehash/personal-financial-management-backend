using MediatR;

namespace PersonalFinancialManagement.Application.Features.Commands.DeleteTransaction;

public record DeleteTransactionCommand(Guid Id) : IRequest;
