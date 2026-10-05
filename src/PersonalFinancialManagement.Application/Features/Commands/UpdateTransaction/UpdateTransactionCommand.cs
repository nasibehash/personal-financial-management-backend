using MediatR;
using PersonalFinancialManagement.Application.Common.Validation;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Features.Commands.UpdateTransaction;

// Replaces the transaction's fields. A null Date keeps the existing date.
public record UpdateTransactionCommand(
    Guid Id,
    TransactionType Type,
    decimal Amount,
    Guid AccountId,
    Guid? CategoryId = null,
    Guid? DestinationAccountId = null,
    DateTime? Date = null,
    string? Description = null) : IRequest<TransactionDto>, ITransactionInput;
