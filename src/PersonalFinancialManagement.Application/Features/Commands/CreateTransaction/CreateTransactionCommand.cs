using MediatR;
using PersonalFinancialManagement.Application.Common.Validation;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Features.Commands.CreateTransaction;

// Date defaults to now. Source records how the transaction was entered (manual, text or voice).
public record CreateTransactionCommand(
    TransactionType Type,
    decimal Amount,
    Guid AccountId,
    Guid? CategoryId = null,
    Guid? DestinationAccountId = null,
    DateTime? Date = null,
    string? Description = null,
    TransactionSource Source = TransactionSource.Manual) : IRequest<TransactionDto>, ITransactionInput;
