using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.DTOs;

// The transaction as understood from the text, with names already matched to the user's own
// accounts and categories (ids are null when nothing matched). Method is "ai" or "rules".
public record ParsedTransactionDto(
    TransactionType Type,
    decimal Amount,
    DateTime Date,
    string? Description,
    Guid? AccountId,
    string? AccountName,
    Guid? DestinationAccountId,
    string? DestinationAccountName,
    Guid? CategoryId,
    string? CategoryName,
    string Method);

// Transcript is only set for voice input. Transaction is null when the request was a preview.
public record AiTransactionResultDto(string? Transcript, ParsedTransactionDto Draft, TransactionDto? Transaction);
