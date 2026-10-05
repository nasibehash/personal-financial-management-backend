using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Common.Validation;

// The fields shared by every request that creates or edits a transaction.
public interface ITransactionInput
{
    TransactionType Type { get; }
    decimal Amount { get; }
    Guid AccountId { get; }
    Guid? CategoryId { get; }
    Guid? DestinationAccountId { get; }
    DateTime? Date { get; }
    string? Description { get; }
}
