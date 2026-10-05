using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Api.Contracts;

// Body for creating and updating a transaction.
// Income/Expense need categoryId; Transfer needs destinationAccountId instead.
public record TransactionRequest(
    TransactionType Type,
    decimal Amount,
    Guid AccountId,
    Guid? CategoryId = null,
    Guid? DestinationAccountId = null,
    DateTime? Date = null,
    string? Description = null);
