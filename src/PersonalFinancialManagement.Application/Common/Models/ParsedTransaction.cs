using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Common.Models;

// What the user already has, so the parser can map free text onto real category and account names.
public record TransactionParsingContext(
    DateTime Now,
    IReadOnlyList<string> IncomeCategories,
    IReadOnlyList<string> ExpenseCategories,
    IReadOnlyList<string> AccountNames);

// A transaction extracted from free text. Names are exactly as they appear in the parsing context
// (null when nothing matched). Method is "ai" or "rules", depending on what produced the result.
public record ParsedTransaction(
    TransactionType Type,
    decimal Amount,
    string? CategoryName,
    string? AccountName,
    string? DestinationAccountName,
    string? Description,
    DateTime? Date,
    string Method);
