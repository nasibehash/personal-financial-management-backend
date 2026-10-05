using MediatR;

namespace PersonalFinancialManagement.Application.Features.Commands.DeleteAccount;

public record DeleteAccountCommand(Guid Id) : IRequest;
