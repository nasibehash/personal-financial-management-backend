using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.DTOs;

// For transfers AccountId is the source and DestinationAccountId the target; for
// income and expenses only AccountId and CategoryId are set.
public record TransactionDto(
    Guid Id,
    TransactionType Type,
    decimal Amount,
    DateTime Date,
    string? Description,
    Guid AccountId,
    string AccountName,
    Guid? DestinationAccountId,
    string? DestinationAccountName,
    Guid? CategoryId,
    string? CategoryName,
    TransactionSource Source);
